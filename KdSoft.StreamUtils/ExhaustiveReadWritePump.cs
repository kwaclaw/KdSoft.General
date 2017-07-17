using System.Threading;
using System.Threading.Tasks;

namespace KdSoft.StreamUtils
{
  /// <summary>
  /// Pumps data from reader to writer until read count is less than requested.
  /// This operation be repeated until the end of data is reached.
  /// </summary>
  /// <remarks>Useful when the reader may run out of data temporarily, such as the <see cref="BufferedReader"/> class.</remarks>
  public class ExhaustiveReadWritePump
  {
    ISerialAsyncReader reader;
    IRandomWriter writer;
    int chunkSize;
    bool isComplete;

    public int ChunkSize {
      get { return chunkSize; }
    }

    // full memory fence is stronger than VolatileRead/Write for guarantee of atomcity
    public bool IsComplete {
      get {
        Interlocked.MemoryBarrier();
        bool value = isComplete;
        Interlocked.MemoryBarrier();
        return value;
      }
      private set {
        Interlocked.MemoryBarrier();
        isComplete = value;
        Interlocked.MemoryBarrier();
      }
    }

    public ExhaustiveReadWritePump(ISerialAsyncReader reader, IRandomWriter writer, int chunkSize) {
      this.reader = reader;
      this.writer = writer;
      this.chunkSize = chunkSize;
      isComplete = false;
    }


    bool StartReadTask(Exhausted exhausted, TaskCreationOptions writeOptions) {
      if (exhausted.Value)
        return false;
      byte[] buffer = new byte[chunkSize];
      var readTask = reader.ReadAsync(buffer, 0, buffer.Length, TaskCreationOptions.AttachedToParent);
      if (readTask == null) {
        IsComplete = true;
        return false;
      }
      readTask.ContinueWith((antecedent) => {
        if (antecedent.Result.IsEnd) {
          IsComplete = true;
          writer.EndWrite(buffer, 0, antecedent.Result.Count, antecedent.Result.Offset);
        }
        else if (antecedent.Result.Count > 0) {
          writer.Write(buffer, 0, antecedent.Result.Count, antecedent.Result.Offset);
          // we just finished a read, so unless we exhausted the data we schedule another one to "replace" it
          if (antecedent.Result.Count < buffer.Length)
            exhausted.Value = true;
          StartReadTask(exhausted, writeOptions);
        }
        else {
          // this chain of tasks ends here - we have exhausted the data supply
          exhausted.Value = true;
        }
      }, TaskContinuationOptions.AttachedToParent);
      return true;
    }

    class Exhausted
    {
      int exhausted = 0;

      public bool Value {
        get {
          // full fence is stronger than VolatileRead for guarantee of atomcity
          Interlocked.MemoryBarrier();
          bool isExhausted = exhausted != 0;
          Interlocked.MemoryBarrier();
          return isExhausted;
        }
        set {
          int newValue = value ? 1 : 0;
          // full fence is stronger than VolatileWrite for guarantee of atomcity
          Interlocked.MemoryBarrier();
          exhausted = newValue;
          Interlocked.MemoryBarrier();
        }
      }
    } 

    /// <summary>
    /// Pumps data from reader to writer until the read count is <c>0</c> or the end is reached.
    /// </summary>
    /// <param name="options">Task creation options for returned task.</param>
    /// <param name="waitForWrites">If <c>false</c> the result task will complete when all reads have finished,
    /// if <c>true</c> the result task will additionally wait until all writes have finished.</param>
    /// <returns>Task instance that can be waited on, or <c>null</c> if already in a completed state. The task result
    /// indicates if the more data are available (<c>true</c>) or if the end of data was reached (<c>false</c>).</returns>
    /// <remarks><list type="bullet">
    ///   <item><description>This is useful for reading from a buffer that could be temporarily exhausted.</description></item>
    ///   <item><description>The behaviour might be unpredictable if pump tasks are executing concurrently, it is best
    ///   to wait for the previous PumpData task's completion before calling PumpData again.</description></item>
    /// </list></remarks>
    public Task<bool> PumpData(TaskCreationOptions options, bool waitForWrites) {
      if (IsComplete)
        return null;

      var writeOptions = waitForWrites ? TaskCreationOptions.AttachedToParent : TaskCreationOptions.None;
      var task = Task.Factory.StartNew(() => {
        // we create at most reader.MaxConcurrency read tasks
        Exhausted exhausted = new Exhausted();
        for (int concurrency = 0; concurrency < Constants.DefaultIOConcurrency; concurrency++) {
          if (!StartReadTask(exhausted, writeOptions))
            break;
        }
      }, options);

      var contOptions = TaskContinuationOptions.ExecuteSynchronously;
      if ((options & TaskCreationOptions.AttachedToParent) == TaskCreationOptions.AttachedToParent)
        contOptions |= TaskContinuationOptions.AttachedToParent;
      var resultTask = task.ContinueWith<bool>((antecedent) => {
        return !IsComplete;  // true when we can continue
      }, contOptions);

      return resultTask;
    }
  }
}
