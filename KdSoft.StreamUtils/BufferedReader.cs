using System;
using System.Threading.Tasks;

namespace KdSoft.StreamUtils
{
  /// <summary>
  /// Delegate for initiating asynchronous serial data requests.
  /// </summary>
  /// <param name="count">Amount of data requested.</param>
  /// <returns>Task instance that can be waited on, or <c>null</c> if the end of the source entity was already reached.</returns>
  public delegate Task SerialDataRequestHandler(int count);

  /// <summary>
  /// Delegate for initiating asynchronous random data requests.
  /// </summary>
  /// <param name="buffer">Byte buffer to receive the requested data.</param>
  /// <param name="start">Position in buffer to start copying data to.</param>
  /// <param name="count">Amount of data requested.</param>
  /// <param name="sourceOffset">Offset in source entity to read the requested data from.</param>
  /// <param name="options">Task creation options to use.</param>
  /// <returns>Task instance that can be waited on.</returns>
  public delegate Task<IOResult> RandomDataRequestHandler(byte[] buffer, int start, int count, long sourceOffset, TaskCreationOptions options);

  /// <summary>
  ///	Implements <see cref="IReader"/> based interfaces through a read cache. Buffers a specified amount of data in a contiguous cache
  /// and allows most (random) re-read requests to be satisfied from this cache, while other random read requests must be satisfied
  /// through a callback to a <see cref="RandomDataRequestHandler"/>.
  /// </summary>
  /// <remarks>Thread-safe, reads and writes are serialized through locks, but writes must maintain the proper order of data buffers.</remarks>
  /// <remarks>This can be used to add (partial) random read capabilities to non-seekable streams.</remarks>
  public class BufferedReader: ISerialReader, ISerialAsyncReader, IRandomReader, IRandomAsyncReader, IFilterWriter
  {
    long writeOffset;
    long readOffset;
    bool writeComplete;
    bool readComplete;
    byte[] finalBuffer;
    int finalBufferOffset;
    RingBuffer<byte> tailBuffer;
    int requestThreshold;
    Exception serialRequestError;
    readonly object syncObj = new object();

    /// <summary>Constructor.</summary>
    /// <param name="bufferSize">Amount of data to buffer.</param>
    /// <param name="requestThreshold">Threshold value that triggers a request for more serial data.</param>
    /// <remarks></remarks>
    public BufferedReader(int bufferSize, int requestThreshold) {
      if (bufferSize < 0)
        throw new ArgumentException("Buffer size must be > 0.", "bufferSize");
      if (requestThreshold < 0)
        throw new ArgumentException("Request threshold must be > 0.", "requestThreshold");
      if (requestThreshold >= bufferSize)
        throw new ArgumentException("Request threshold must be less than buffer size.");
      this.requestThreshold = requestThreshold;
      tailBuffer = new RingBuffer<byte>(bufferSize);
    }

    #region Public API

    /// <summary>Size of buffer.</summary>
    public int BufferSize {
      get { return tailBuffer.Capacity; }
    }

    /// <summary>
    /// Occurs when sequential (not random!) reading consumes sufficient data to let the size of the data
    /// remaining in the buffer drop below the <see cref="RequestThreshold">request threshold</see>.
    /// </summary>
    /// <remarks>Task faults returned from the event handler will set the <see cref="DataRequestError"/>
    /// property, which will cause further serial (but not random) reads to fail.</remarks>
    public SerialDataRequestHandler SerialDataRequested { get; set; }

    /// <summary>
    /// Occurs when a random *asynchronous* read request cannot fully be satisfied from the buffer.
    /// </summary>
    /// <remarks>><list type="bullet">
    ///   <item><description>Random *synchronous* read requests will never call this handler.</description></item>
    ///   <item><description>This class will pass any task faults returned from the event handler to the
    ///   <see cref="ReadAsync(byte[],int,int,TaskCreationOptions)">originating random read request</see></description></item>.
    /// </list></remarks>
    public RandomDataRequestHandler RandomDataRequested { get; set; }

    /// <summary>
    /// Request threshold. When consuming data through sequential reading, new data should be written
    /// once the read offset approaches the write offset, so that we don't run out of data to read.
    /// When the level of readable data drops below the request threshold, a data request will be triggered.
    /// See <see cref="SerialDataRequested"/>.
    /// </summary>
    /// <value>The request threshold.</value>
    public int RequestThreshold {
      get { return requestThreshold; }
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

    /// <summary>
    /// Last error returned by a <see cref="SerialDataRequested"/> handler task.
    /// </summary>
    public Exception DataRequestError {
      get { lock (syncObj) return serialRequestError; }
    }

    #endregion

    #region IFilterWriter

    /// <summary>Performs the actual write to the internal cache.</summary>
    /// <param name="buffer">Byte buffer that contains the data to write.</param>
    /// <param name="start">Start index of data.</param>
    /// <param name="count">Amount of data to write.</param>
    /// <returns>Number of bytes written. May be less that <c>count</c> if the
    /// buffer does not have sufficient free space.</returns>
    /// <remarks>Must be performed under lock protection.</remarks>
    int WriteBuffer(byte[] buffer, int start, int count) {
      if (count == 0) {
        return 0;
      }
      int written = tailBuffer.Add(buffer, start, count);
      writeOffset += written;
      return written;
    }

    /// <summary>Supplies sequential data to the buffer.</summary>
    /// <param name="buffer">Byte buffer that contains the data to write.</param>
    /// <param name="start">Start index of data.</param>
    /// <param name="count">Amount of data to write.</param>
    /// <returns>Number of bytes written. May be less that <c>count</c> if the
    /// buffer does not have sufficient free space.</returns>
    public int Write(byte[] buffer, int start, int count) {
      lock (syncObj) {
        if (writeComplete)
          throw new InvalidOperationException("Writing is already complete.");
        int written = WriteBuffer(buffer, start, count);
        return written;
      }
    }

    /// <summary>Supplies sequential data to the buffer, indicating the last buffer and completing write operations.</summary>
    /// <param name="buffer">Byte buffer that contains the data to write.</param>
    /// <param name="start">Start index of data.</param>
    /// <param name="count">Amount of data to write.</param>
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

    #endregion

    #region IReader Members

    /// <summary>
    /// Inidicates if the size of the data is known and returns it if true.
    /// </summary>
    /// <param name="size">Total size of data - same as last stream position. Only valid if result is <c>true</c>.</param>
    /// <returns><c>true</c> if the data size is known; otherwise, <c>false</c>.</returns>
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

    IOResult InternalRead(byte[] buffer, int start, int count) {
      if (count > tailBuffer.Capacity)
        throw new ArgumentException("The number of bytes requested must not exceed the buffer's capacity.", "count");
      // let's check if the final buffer should be written
      CheckFinalbuffer();
      bool allWritten = writeComplete && finalBuffer == null;

      int readCount = tailBuffer.Take(buffer, start, count);
      bool isEnd = allWritten ? readCount < count : false;
      IOResult readResult = new IOResult(readOffset, readCount, isEnd);
      readOffset += readCount;
      if (isEnd)
        readComplete = true;
      else
        CheckFinalbuffer();

      if (readResult.Count == 0 && serialRequestError != null)
        throw new InvalidOperationException("Error in BufferedReader.", serialRequestError);

      if (!writeComplete) {
        var requested = SerialDataRequested;
        if (requested != null && tailBuffer.Count < requestThreshold) {
          var requestTask = requested(tailBuffer.AvailableToWrite);
          if (requestTask != null)
            requestTask.ContinueWith((rtask) => { lock (syncObj) serialRequestError = rtask.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        }
      }
      return readResult;
    }

    public IOResult Read(byte[] buffer, int start, int count) {
      lock (syncObj) {
        if (readComplete)
          return new IOResult(readOffset, 0, true);
        var readResult = InternalRead(buffer, start, count);
        return readResult;
      }
    }

    #endregion

    #region IRandomReader Members

    IOResult InternalRead(byte[] buffer, int start, int count, long sourceOffset) {
      bool allWritten = writeComplete && finalBuffer == null;
      int available = tailBuffer.AvailableToRead;
      long startOffset = writeOffset - available;  // how far back does the tailBuffer have data
      if (startOffset < 0)     // if the available data is larger than the data written so far (should not happen)
        startOffset = 0;

      int bufferOffset;
      long delta = sourceOffset - startOffset;
      if (delta >= available)  // read start beyond buffer range
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
      bool isEnd = allWritten && (readCount < count || sourceOffset == writeOffset);
      return new IOResult(sourceOffset, readCount, isEnd);
    }

    /// <summary>
    /// Reads <c>count</c> bytes from underlying entity (e.g. stream) starting at position <c>sourceOffset</c>. Called synchronously.
    /// </summary>
    /// <param name="buffer">Byte buffer to write the data into.</param>
    /// <param name="start">Position in buffer to start writing to.</param>
    /// <param name="count">Number of bytes to read.</param>
    /// <param name="sourceOffset">Position in underlying entity to start reading from.</param>
    /// <returns>An <see cref="IOResult"/> instance.</returns>
    /// <remarks>This implementation will limit - if applicable -  the range of positions to read from.	Based on the amount of buffered
    /// data, the returned offset and count can be affected - see <see cref="IOResult"/>. It ignores serial data request errors.</remarks>
    public IOResult Read(byte[] buffer, int start, int count, long sourceOffset) {
      lock (syncObj) {
        return InternalRead(buffer, start, count, sourceOffset);
      }
    }

    #endregion

    #region ISerialAsyncReader Members

    /// <summary>
    /// Reads up to <c>count</c> bytes sequentially from underlying entity (e.g. stream). Called asynchronously.
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
    ///   <item><description>This implementation will actually run synchronously.</description></item>
    ///   <item><description>If the task result returns a count of <c>0</c>, but does not indicate the end of data, then this means
    ///   that currently the buffer is exhausted, but more data may be available in the future.</description></item>
    ///   <item><description>If the returned count is less than requested this does not mean that the end of the data was reached - see <see cref="IOResult"/></description></item>
    /// </list></remarks>
    public Task<IOResult> ReadAsync(byte[] buffer, int start, int count, TaskCreationOptions options) {
      TaskCompletionSource<IOResult> taskSource = null;
      try {
        IOResult readResult;
        lock (syncObj) {
          if (readComplete) {
            return null;
          }
          taskSource = new TaskCompletionSource<IOResult>(options);
          readResult = InternalRead(buffer, start, count);
        }
        taskSource.SetResult(readResult);
      }
      catch (Exception ex) {
        if (taskSource != null)
          taskSource.SetException(ex);
      }
      return taskSource == null ? null : taskSource.Task;
    }

    #endregion

    #region IRandomAsyncReader Members

    /// <summary>
    /// Reads <c>count</c> bytes from underlying entity (e.g. stream) starting at position <c>sourceOffset</c>. Called asynchronously.
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
    ///   <item><description>This implementation will run synchronously when all the requested data is available from the buffer. If some of the data
    ///   are not in the buffer (anymore), it will delegate the request to the asynchronous <see cref="RandomDataRequested"/> handler.</description></item>
    /// 	<item><description>If no <see cref="RandomDataRequested"/> handler is set, then the implementation will limit the range of positions to read from,
    /// 	based on the amount of buffered data. The returned offset and count can be affected - see <see cref="IOResult"/>.</description></item>
    /// 	<item><description>Serial data request errors are ignored, but errors in the <see cref="RandomDataRequested"/> handler are passed to the caller.</description></item>
    /// </list></remarks>
    public Task<IOResult> ReadAsync(byte[] buffer, int start, int count, long sourceOffset, TaskCreationOptions options) {
      try {
        IOResult readResult;
        lock (syncObj) {
          readResult = InternalRead(buffer, start, count, sourceOffset);
        }
        // we return the buffer read result if we don't have a random request handler, or if that handler would not be called;
        // if the returned count is less than requested than the request could have been truncated at either end of the range;
        // if it was truncated at the start, then the Offset was changed as well, however, if it was truncated at the end
        // (sourceOffset unchanged), then it might have hit the end of the source, in which case we cannot read the rest;
        var randomRequested = RandomDataRequested;
        bool haveAllData = readResult.Count == count || (readResult.IsEnd && readResult.Offset == sourceOffset) || count == 0;
        if (randomRequested == null || haveAllData) {
          var taskSource = new TaskCompletionSource<IOResult>(options);
          taskSource.SetResult(readResult);
          return taskSource.Task;
        }
        else if (readResult.Count > 0) {  // readResult.Count < count && (!readResult.IsEnd || readResult.Offset > sourceOffset)
          // it is possible that the request was truncated at both ends, if the total buffer size is less than the request's range,
          // so we would have to issue *two* calls to the random request handler, but we won't, we give priority to the "older" request part;
          var delta = (int)(readResult.Offset - sourceOffset);
          if (delta > 0) {  // if we truncated from the start, then we have to move the data forward to make room for the handler
            Buffer.BlockCopy(buffer, start, buffer, start + delta, readResult.Count);
            var randomTask = randomRequested(buffer, start, delta, sourceOffset, options);
            return randomTask.ContinueWith<IOResult>((rtask) => {  // make sure we preserve the value of readResult.IsEnd
              var result = rtask.Result;  // throws if rt had error
              if (readResult.IsEnd)
                return new IOResult(result.Offset, result.Count, true);
              return result;
            }, (TaskContinuationOptions)options);
          }
          else {  // we truncated from the end of the request
            start += readResult.Count;
            sourceOffset += readResult.Count;
            count -= readResult.Count;
            return randomRequested(buffer, start, count, sourceOffset, options);
          }
        }
        else  // readResult.Count == 0 && count > 0
          return randomRequested(buffer, start, count, sourceOffset, options);
      }
      catch (Exception ex) {
        var taskSource = new TaskCompletionSource<IOResult>(options);
        taskSource.SetException(ex);
        return taskSource.Task;
      }
    }

    #endregion
  }
}
