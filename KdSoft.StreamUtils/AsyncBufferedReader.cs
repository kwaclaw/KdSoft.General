using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KdSoft.StreamUtils
{
  /// <summary>
  ///	<see cref="IRandomAsyncReader"/> implementation that buffers a specified amount of data in a contiguous cache
  /// and allows random read requests to be satisfied from this cache.
  /// </summary>
  /// <remarks>Thread-safe, reads and writes are serialized through locks, but writes must maintain the proper order of data buffers.</remarks>
  /// <remarks>This can be used to add (partial) random read capabilities to non-seekable streams.</remarks>
  public class AsyncBufferedReader: ISerialAsyncReader, IRandomAsyncReader
  {
    long writeOffset;
    long readOffset;
    bool writeComplete;
    bool readComplete;
    byte[] finalBuffer;
    int finalBufferOffset;
    readonly RingBuffer<byte> tailBuffer;
    readonly Queue<WaitingRequest> waitingRequests;  // queue of read requests waiting for data to be written
    readonly List<WaitingRequest> completedRequests; // list of completed read requests
    readonly object syncObj = new object();

    class WaitingRequest
    {
      public TaskCompletionSource<IOResult> TaskSource { get; set; }
      public byte[] Buffer { get; set; }
      public int Start { get; set; }
      public int Count { get; set; }
      public long ReadOffset { get; set; }
      public bool IsEnd { get; set; }
    }

    /// <summary>Constructor.</summary>
    /// <param name="bufferSize">Amount of data to buffer.</param>
    public AsyncBufferedReader(int bufferSize) {
      writeOffset = 0;
      readOffset = 0;
      writeComplete = false;
      readComplete = false;
      finalBuffer = null;
      finalBufferOffset = 0;
      tailBuffer = new RingBuffer<byte>(bufferSize);
      waitingRequests = new Queue<WaitingRequest>();
      completedRequests = new List<WaitingRequest>(Constants.DefaultIOConcurrency);
    }

    #region Public API

    /// <summary>Size of buffer.</summary>
    public int BufferSize {
      get { return tailBuffer.Capacity; }
    }

    // requires lock on syncObj
    int ProcessWaitingRequests(List<WaitingRequest> completedRequests) {
      int totalRead = 0;
      bool allWritten = writeComplete && finalBuffer == null;
      // we must process the wait queue in order and stop at the first failure
      while (waitingRequests.Count > 0) {
        var wr = waitingRequests.Peek();
        if (tailBuffer.Count >= wr.Count || allWritten) {
          int readCount = tailBuffer.Take(wr.Buffer, wr.Start, wr.Count);
          wr.ReadOffset = readOffset;
          readOffset += readCount;
          if (allWritten) {
            wr.IsEnd = readComplete || wr.Count < readCount;
            wr.Count = readCount;
            if (wr.IsEnd && !readComplete)
              readComplete = true;
          }
          completedRequests.Add(waitingRequests.Dequeue());
          totalRead += readCount;
        }
        else
          break;
      }
      return totalRead;
    }

    /// <summary>Performs the actual write to the internal cache.</summary>
    /// <param name="buffer">Byte buffer that contains the data to write.</param>
    /// <param name="start">Start index of data.</param>
    /// <param name="count">Amount of data to write.</param>
    /// <returns>Number of bytes written. May be less that <c>count</c> if the
    /// buffer does not have sufficient free space.</returns>
    /// <remarks>Must be performed under lock protection.</remarks>
    int WriteBuffer(byte[] buffer, int start, int count) {
      int totalWritten = 0;
      WaitingRequest[] finished = null;
      try {
        if (count == 0) {
          ProcessWaitingRequests(completedRequests);
          return 0;
        }
        completedRequests.Clear();
        int toWrite = count;
        int written;
        do {
          written = tailBuffer.Add(buffer, start, toWrite);
          totalWritten += written;
          start += written;
          toWrite -= written;
          ProcessWaitingRequests(completedRequests);  // make space for adding data
        } while (written > 0 && toWrite > 0);

        writeOffset += totalWritten;
        finished = completedRequests.ToArray();
        return totalWritten;
      }
      finally {
        if (finished != null)
          for (int indx = 0; indx < finished.Length; indx++) {
            var cr = finished[indx];
            cr.TaskSource.SetResult(new IOResult(cr.ReadOffset, cr.Count, cr.IsEnd));
          }
      }
    }

    /// <summary>Supplies sequential data to the buffer.</summary>
    /// <param name="buffer">Byte buffer that contains the data to write.</param>
    /// <param name="start">Start index of data.</param>
    /// <param name="count">Amount of data to write.</param>
    /// <returns>Number of bytes written. May be less that <c>count</c> if the
    /// buffer does not have sufficient free space.</returns>
    /// <remarks>Not thread-safe.</remarks>
    public int Write(byte[] buffer, int start, int count) {
      lock (syncObj) {
        if (writeComplete)
          throw new InvalidOperationException("Writing is already complete.");
        return WriteBuffer(buffer, start, count);
      }
    }

    /// <summary>Supplies sequential data to the buffer, indicating the last buffer and completing write operations.</summary>
    /// <param name="buffer">Byte buffer that contains the data to write.</param>
    /// <param name="start">Start index of data.</param>
    /// <param name="count">Amount of data to write.</param>
    /// <remarks>Not thread-safe.</remarks>
    public void FinalWrite(byte[] buffer, int start, int count) {
      lock (syncObj) {
        if (writeComplete)
          throw new InvalidOperationException("Writing is already complete.");
        int written = WriteBuffer(buffer, start, count);
        int delta = count - written;
        if (delta > 0) {
          finalBuffer = new byte[delta];
          Buffer.BlockCopy(buffer, start + written, finalBuffer, 0, delta);
        }
        writeComplete = true;
      }
    }

    /// <summary>
    /// Gets a value indicating whether this instance has completed writing.
    /// </summary>
    /// <value><c>true</c> if this instance is complete; otherwise, <c>false</c>.</value>
    public bool IsComplete {
      get {
        lock (syncObj) {
          return writeComplete && finalBuffer == null;
        }
      }
    }

    #endregion

    #region IReader Members

    /// <summary>
    /// Inidicates if the size of the data is known and returns it if true.
    /// </summary>
    /// <param name="size">Total size of data - same as last stream position. Only valid if result is <c>true</c>.</param>
    /// <returns>
    /// 	<c>true</c> if the data size is known; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>Implementation may decide to return the largest known size, if result is <c>false</c>.</remarks>
    public bool GetSize(out long size) {
      lock (syncObj) {
        size = writeOffset;
        return writeComplete;
      }
    }

    #endregion

    #region ISerialReader Members

    void CheckFinalbuffer() {
      if (writeComplete && finalBuffer != null) {
        int written = WriteBuffer(finalBuffer, finalBufferOffset, finalBuffer.Length - finalBufferOffset);
        finalBufferOffset += written;
        if (finalBufferOffset >= finalBuffer.Length) {
          finalBuffer = null;
        }
      }
    }

    /// <summary>
    /// Reads up to <c>count</c> bytes sequentially from underlying entity (e.g. stream).
    /// Performed asynchronously.
    /// </summary>
    /// <param name="buffer">Byte buffer to write the data into.</param>
    /// <param name="start">Position in buffer to start writing to.</param>
    /// <param name="count">Maximum number of bytes to read.</param>
    /// <param name="options">Task creation options to use.</param>
    /// <returns>
    /// Scheduled task instance that can be waited on, or <c>null</c> if reading is already complete (end of data encountered).
    /// Task result contains <see cref="IOResult"/> details.
    /// </returns>
    /// <remarks><list type="bullet">
    /// 	<item>If the concurrency level exceeds the maximum, this method will not block but return the new concurrency level.</item>
    /// 	<item>If the returned count is less than requested this does not mean that the end of the data was reached - see <see cref="IOResult"/></item>
    /// 	<item>However, in this implementation a read task will wait until it can read all requested data, except when it is the end of data.</item>
    /// </list></remarks>
    public Task<IOResult> ReadAsync(byte[] buffer, int start, int count, TaskCreationOptions options) {
      TaskCompletionSource<IOResult> taskSource = null;
      WaitingRequest wr = null;
      IOResult readResult = new IOResult();
      WaitingRequest[] finished = null;
      try {
        lock (syncObj) {
          if (readComplete) {
            return null;
          }
          completedRequests.Clear();
          if (count > tailBuffer.Capacity)
            throw new ArgumentException("The number of bytes requested must not exceed the buffer's capacity.", "count");

          // waiting requests must be processed in order to ensure data integrity - we read sequentially
          while (ProcessWaitingRequests(completedRequests) > 0) {
            CheckFinalbuffer();
          }

          taskSource = new TaskCompletionSource<IOResult>(options);
          // if no requests are waiting we don't need to queue up and can try to process synchronously
          bool allWritten = writeComplete && finalBuffer == null;
          if (waitingRequests.Count == 0 && (tailBuffer.Count >= count || allWritten)) {
            int readCount = tailBuffer.Take(buffer, start, count);
            bool isEnd = allWritten ? readCount < count : false;
            readResult = new IOResult(readOffset, readCount, isEnd);
            readOffset += readCount;
            if (isEnd)
              readComplete = true;
            else
              CheckFinalbuffer();
          }
          else {
            wr = new WaitingRequest() {
              TaskSource = taskSource,
              Buffer = buffer,
              Start = start,
              Count = count,
              IsEnd = false
            };
            waitingRequests.Enqueue(wr);
          }
          finished = completedRequests.ToArray();
        }
      }
      finally {
        // if completed synchronously set result outside of lock as this might trigger a call to Close() and create a deadlock
        if (wr == null && taskSource != null)
          taskSource.SetResult(readResult);
        if (finished != null)
          for (int indx = 0; indx < finished.Length; indx++) {
            var cr = finished[indx];
            cr.TaskSource.SetResult(new IOResult(cr.ReadOffset, cr.Count, cr.IsEnd));
          }
      }
      return taskSource != null ? taskSource.Task : null;
    }

    #endregion

    #region IRandomReader Members

    IOResult InternalRead(byte[] buffer, int start, int count, long sourceOffset) {
      if (count > tailBuffer.Capacity)
        throw new ArgumentException("The number of bytes requested must not exceed the buffer's capacity.", "count");
      bool allWritten = writeComplete && finalBuffer == null;

      long startOffset = writeOffset - tailBuffer.Count;
      int bufferOffset;
      long delta = sourceOffset - startOffset;
      if (delta >= tailBuffer.Count)  // read range beyond buffer range
        return new IOResult(writeOffset, 0, allWritten);
      else if (delta >= 0)  // start reading at an offset in the buffer
        bufferOffset = (int)delta;
      else {  // delta < 0, sourceOffset before startOffset, must start at startOffset and reduce count by offset distance
        sourceOffset = startOffset;
        bufferOffset = 0;
        long tmpCount = count + delta;
        if (tmpCount < 0)  // distance too far, read range outside of buffer range
          return new IOResult(startOffset, 0, false);
        count = (int)tmpCount;
      }

      int readCount;
      if (count > 0)
        readCount = tailBuffer.Read(buffer, start, count, bufferOffset);
      else
        readCount = 0;
      bool isEnd = allWritten && readCount < count;
      return new IOResult(sourceOffset, readCount, isEnd);
    }

    /// <summary>
    /// Reads <c>count</c> bytes from underlying entity (e.g. stream) starting at position <c>offset</c>.
    /// Performed synchronously in this implementation, despite the asynchronous interface.
    /// </summary>
    /// <param name="buffer">Byte buffer to write the data into.</param>
    /// <param name="start">Position in buffer to start writing to.</param>
    /// <param name="count">Number of bytes to read.</param>
    /// <param name="sourceOffset">Position in underlying entity to start reading from.</param>
    /// <param name="options">Task creation options to use.</param>
    /// <returns>
    /// Scheduled task instance that can be waited on (never <c>null</c>). Task result contains <see cref="IOResult"/> details.
    /// </returns>
    /// <remarks><list type="bullet">
    /// 		<item>If the concurrency level exceeds the maximum, this method will not block but just return the new concurrency level.</item>
    /// 		<item>Implementation may limit the range of positions to read from, e.g. limited to XX bytes before the last
    /// sequentially read position. The returned offset and count can be affected - see <see cref="IOResult"/>.</item>
    /// 	</list></remarks>
    public Task<IOResult> ReadAsync(byte[] buffer, int start, int count, long sourceOffset, TaskCreationOptions options) {
      var taskSource = new TaskCompletionSource<IOResult>(options);
      try {
        IOResult readResult;
        lock (syncObj) {
          readResult = InternalRead(buffer, start, count, sourceOffset);
        }
        taskSource.SetResult(readResult);
      }
      catch (Exception ex) {
        taskSource.SetException(ex);
      }
      return taskSource.Task;
    }

    #endregion
  }
}
