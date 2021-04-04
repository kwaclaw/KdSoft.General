using System;
using System.Threading.Tasks;

namespace KdSoft.StreamUtils
{
  /* Description of algorithm for AsyncReadWritePump.PumpData:
   * - when the last read is completed (and identified as such) no more reads can be scheduled, and any
   *   reads already active for a "later" offset must be cancelled or ignored (incl. the resulting writes);
   * - any error or cancellation will disallow more reads to be scheduled, and active reads will ignore their continuations,
   *   meaning that they will not schedule any more writes; active writes will complete, but no more pending writes will be activated;
   * - the write tasks are children of the associated read tasks, and these are children of a pump task, so when a pump task
   *   completes then all its read and write tasks must have completed also;
   * - completion of the last pump task means therefore that all is complete;
   * - even when the write associated with the last read fully completes, we may still have pending and active writes
   *   which are all allowed as they cannot cover a "later" offset - because this case was already handled for reads;
   * - We have the following states:
   *   . reading: active reads > 0, final read incomplete;
   *       PumpData will schedule reads, reads will schedule writes and writes will schedule pending writes
   *   . reading complete: last read complete; writes still active;
   *       PumpData will stop scheduling reads and return false, reads with an index > endReadIndex will ignore their continuations,
   *       writes with an index > endReadIndex will not schedule any more more pending writes
   *   . all complete: all writes have completed;
   *       the last pump task will complete and continuations can be scheduled to handle completion
   *   . cancel or error: can happen anytime;
   *       PumpData will return false, no more reads or writes will scheduled, continuations will be ignored
   */

  /// <summary>
  /// "Data Pump" that reads sequentially, but asynchronously, from a (<see cref="ISerialAsyncReader"/> and writes randomly
  /// to an <see cref="IRandomWriter"/>, as the read requests may complete out of order.
  /// </summary>
  public class AsyncReadWritePump
  {
    ISerialAsyncReader reader;
    IRandomWriter writer;
    int chunkSize;

    struct WriteRequest
    {
      public byte[] Buffer { get; set; }
      public int Count { get; set; }
      public long Offset { get; set; }
      public bool IsEnd { get; set; }
      public int Index { get; set; }
    }

    #region Synchronized Access

    readonly object syncObj = new object();

    int readIndex;
    bool endReadComplete;
    int endReadIndex;
    long totalSize;

    bool isCanceled;
    Exception error;

    #endregion

    /// <summary>
    /// Constructor for <see cref="AsyncReadWritePump"/>
    /// </summary>
    /// <param name="reader">Source of data to read.</param>
    /// <param name="writer">Sink for data to be written to.</param>
    /// <param name="chunkSize">Size of data to read or write in one request.</param>
    public AsyncReadWritePump(ISerialAsyncReader reader, IRandomWriter writer, int chunkSize) {
      if (reader == null)
        throw new ArgumentNullException("reader");
      if (writer == null)
        throw new ArgumentNullException("writer");
      this.reader = reader;
      this.writer = writer;
      this.chunkSize = chunkSize;
      readIndex = 0;
      endReadComplete = false;
      endReadIndex = 0;
      totalSize = 0;
      isCanceled = false;
      error = null;
    }

    bool Write(byte[] buffer, int count, long offset, bool isEnd, int index) {
      bool success;
      try {
        if (isEnd)
          success = writer.EndWrite(buffer, 0, count, offset);
        else
          success = writer.Write(buffer, 0, count, offset);
        if (!success) {
          lock (syncObj) {
            if (endReadComplete && index <= endReadIndex) {
              throw new InvalidOperationException("Writer reached completed state before it should.");
            }
          }
          return success;
        }
      }
      catch (Exception ex) {
        lock (syncObj) {
          error = ex;
        }
        success = false;
      }
      return success;
    }

    Task<IOResult> StartReadTask(byte[] buffer, PumpResult pumpResult) {
      var readTask = reader.ReadAsync(buffer, 0, chunkSize, TaskCreationOptions.AttachedToParent);
      if (readTask == null)
        return null;
      int index;
      lock (syncObj)
        index = readIndex++;
      readTask.ContinueWith((antecedent) => {
        bool doWrite = false;
        lock (syncObj) {
          if (error != null || isCanceled || (endReadComplete && index > endReadIndex))
            return;
          switch (antecedent.Status) {
            case TaskStatus.Faulted:
              error = antecedent.Exception;
              break;
            case TaskStatus.Canceled:
              isCanceled = true;
              break;
            case TaskStatus.RanToCompletion:
              long offset = antecedent.Result.Offset;
              if (offset < pumpResult.Offset)
                pumpResult.Offset = offset;
              int readCount = antecedent.Result.Count;
              pumpResult.Count += readCount;
              bool isEnd = antecedent.Result.IsEnd;
              if (isEnd) {
                pumpResult.IsEnd = true;
              }
              // Debug.Assert(!endReadComplete || (endReadComplete && !isEnd) || (endReadComplete && isEnd && readCount == 0));
              bool firstEnd = !endReadComplete && isEnd;

              if (readCount > 0 || firstEnd) {
                if (isEnd) {
                  totalSize = offset + readCount;
                  endReadComplete = true;
                  endReadIndex = index;
                }
                doWrite = true;
              }
              break;
            default:
              break;
          }
        }

        if (doWrite) {
          Write(buffer, antecedent.Result.Count, antecedent.Result.Offset, antecedent.Result.IsEnd, index);
        }
      }, TaskContinuationOptions.AttachedToParent);
      return readTask;
    }

    class PumpResult
    {
      public long Offset { get; set; }
      public int Count { get; set; }
      public bool IsEnd { get; set; }
    }

    /// <summary>
    /// Pumps a specific amount of data from the source <see cref="ISerialAsyncReader"/> to the target <see cref="IRandomWriter"/>.
    /// </summary>
    /// <param name="count">The number of bytes to pump.</param>
    /// <param name="options">Task creation options.</param>
    /// <returns>A <see cref="Task{IOResult}"/> instance that can be waited on. The pump task will complete when all read
    /// and write tasks created as a result of this call have finished. The task result will contain the offset of the starting read
    /// and the accumulated count of all reads, as well as an indicator if the end of the data was reached.</returns>
    public Task<IOResult> PumpData(int count, TaskCreationOptions options = TaskCreationOptions.None) {
      lock (syncObj) {
        if (endReadComplete)
          return null;
      }

      var task = Task.Factory.StartNew<PumpResult>(() => {
        // default offset to max value, as reads finish out of order and we need to find the smallest offset
        var pumpResult = new PumpResult() { Offset = long.MaxValue };
        while (count > 0) {
          int oldCount = count;
          count -= chunkSize;
          byte[] buffer = new byte[count < 0 ? oldCount : chunkSize];
          var readTask = StartReadTask(buffer, pumpResult);
          if (readTask == null)
            break;
        }
        return pumpResult;
      }, options);

      var continuationOptions = TaskContinuationOptions.ExecuteSynchronously;
      if ((TaskCreationOptions.AttachedToParent & options) == TaskCreationOptions.AttachedToParent)
        continuationOptions |= TaskContinuationOptions.AttachedToParent;
      // we are returning the read task results aggregated in pumpResult as IOResult instance
      var resultTask = task.ContinueWith<IOResult>((antecedent) => {
        return new IOResult(antecedent.Result.Offset, antecedent.Result.Count, antecedent.Result.IsEnd);
      }, continuationOptions);

      return resultTask;
    }

    public int ChunkSize {
      get { return chunkSize; }
    }

    public Exception Error {
      get { lock (syncObj) return error; }
    }

    public bool IsCanceled {
      get { lock (syncObj) return isCanceled; }
    }

    public long TotalSize {
      get { lock (syncObj) return totalSize; }
    }
  }
}
