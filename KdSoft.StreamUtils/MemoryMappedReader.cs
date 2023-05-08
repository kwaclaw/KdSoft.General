#if NETSTANDARD2_0 || NETSTANDARD2_1 || NET6_0_OR_GREATER

using System;
using System.IO.MemoryMappedFiles;
using System.Threading.Tasks;

namespace KdSoft.StreamUtils
{
  /// <summary>
  ///	Implements <see cref="IReader"/> based interfaces through a memory mapped cache file. Allows *all* (random) re-read requests 
  ///	to be satisfied from this cache, unlike <see cref="BufferedReader"/>.
  /// </summary>
  /// <remarks>Thread-safe, reads and writes are serialized through locks, but writes must maintain the proper order of data buffers.</remarks>
  /// <remarks>This can be used to add random read capabilities to non-seekable streams.</remarks>
  public class MemoryMappedReader: ISerialReader, ISerialAsyncReader, IRandomReader, IRandomAsyncReader, IFilterWriter, IDisposable
  {
    MemoryMappedViewAccessor accessor;
    MemoryMappedViewStream writer;
    MemoryMappedViewStream reader;
    bool isComplete;
    int requestThreshold;
    int requestSize;
    Exception serialRequestError;
    readonly object syncObj = new object();

    public MemoryMappedReader(MemoryMappedFile memFile, int requestThreshold, int requestSize) {
      if (requestThreshold < 0)
        throw new ArgumentException("Request threshold must be > 0.", "requestThreshold");
      if (requestSize < 0)
        throw new ArgumentException("Request size must be > 0.", "requestSize");
      this.requestThreshold = requestThreshold;
      this.requestSize = requestSize;
      accessor = memFile.CreateViewAccessor(0L, 0L, MemoryMappedFileAccess.Read);
      writer = memFile.CreateViewStream(0L, 0L, MemoryMappedFileAccess.Write);
      reader = memFile.CreateViewStream(0L, 0L, MemoryMappedFileAccess.Read);
    }

    /// <summary>
    /// Occurs when sequential (not random!) reading consumes sufficient data to let the size of the
    /// unread data in the cache drop below the <see cref="RequestThreshold">request threshold</see>.
    /// </summary>
    /// <remarks>Task faults returned from the event handler will set the <see cref="DataRequestError"/>
    /// property, which will cause further serial (but not random) reads to fail.</remarks>
    public SerialDataRequestHandler SerialDataRequested { get; set; }

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
          return isComplete;
        }
      }
    }

    /// <summary>
    /// Last error returned by a <see cref="SerialDataRequested"/> handler task.
    /// </summary>
    public Exception DataRequestError {
      get { lock (syncObj) return serialRequestError; }
    }

#region IReader Members

    /// <inheritdoc/>
    public bool GetSize(out long size) {
      lock (syncObj) {
        size = writer.Position;
        return isComplete;
      }
    }

#endregion

#region ISerialReader Members

    IOResult InternalRead(byte[] buffer, int start, int count) {
      var offset = reader.Position;
      var available = (int)(writer.Position - offset);
      if (available < requestThreshold) {
        if (!isComplete) {
          var requested = SerialDataRequested;
          if (requested != null) {
            var requestTask = requested(requestSize);
            if (requestTask != null)
              requestTask.ContinueWith((rtask) => { lock (syncObj) serialRequestError = rtask.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
          }
        }
        if (available <= 0)
          return new IOResult(offset, 0, isComplete);
      }

      if (available < count)
        count = available;
      var readCount = reader.Read(buffer, start, count);
      bool isEnd = readCount == 0 && isComplete;
      var readResult = new IOResult(offset, readCount, isEnd);
      if (readResult.Count == 0 && serialRequestError != null)
        throw new InvalidOperationException("Error in MemoryMappedReader.", serialRequestError);
      return readResult;
    }

    /// <inheritdoc/>
    public IOResult Read(byte[] buffer, int start, int count) {
      lock (syncObj) {
        return InternalRead(buffer, start, count);
      }
    }

#endregion

#region ISerialAsyncReader Members

    /// <inheritdoc/>
    public Task<IOResult> ReadAsync(byte[] buffer, int start, int count, TaskCreationOptions options) {
      var taskSource = new TaskCompletionSource<IOResult>(options);
      try {
        IOResult readResult;
        lock (syncObj) {
          readResult = InternalRead(buffer, start, count);
          if (readResult.Count == 0 && isComplete)
            return null;
        }
        taskSource.SetResult(readResult);
      }
      catch (Exception ex) {
        taskSource.SetException(ex);
      }
      return taskSource.Task;
    }

#endregion

#region IRandomReader Members

    /// <inheritdoc/>
    public IOResult Read(byte[] buffer, int start, int count, long sourceOffset) {
      lock (syncObj) {
        var available = (int)(writer.Position - sourceOffset);
        if (available <= 0) {
          return new IOResult(writer.Position, 0, isComplete);
        }

        if (available < count)
          count = available;
        int readCount = accessor.ReadArray<byte>(sourceOffset, buffer, start, count);
        bool isEnd = isComplete ? sourceOffset + readCount >= writer.Position : false;
        return new IOResult(sourceOffset, readCount, isEnd);
      }
    }

#endregion

#region IRandomAsyncReader Members

    /// <inheritdoc/>
    public Task<IOResult> ReadAsync(byte[] buffer, int start, int count, long sourceOffset, TaskCreationOptions options) {
      var taskSource = new TaskCompletionSource<IOResult>(options);
      try {
        IOResult readResult = Read(buffer, start, count, sourceOffset);
        taskSource.SetResult(readResult);
      }
      catch (Exception ex) {
        taskSource.SetException(ex);
      }
      return taskSource.Task;
    }

#endregion

#region IFilterWriter Members

    /// <inheritdoc/>
    public int Write(byte[] buffer, int start, int count) {
      lock (syncObj) {
        if (isComplete)
          throw new InvalidOperationException("No more writes allowed.");
        writer.Write(buffer, start, count);
        return count;
      }
    }

    /// <inheritdoc/>
    public void FinalWrite(byte[] buffer, int start, int count) {
      lock (syncObj) {
        if (isComplete)
          throw new InvalidOperationException("No more writes allowed.");
        writer.Write(buffer, start, count);
        isComplete = true;
      }
    }

#endregion

#region IDisposable Members

    /// <inheritdoc/>
    protected virtual void Dispose(bool disposing) {
      if (disposing) {
        lock (syncObj) {
          if (accessor != null)
            accessor.Dispose();
          if (writer != null)
            writer.Dispose();
          if (reader != null)
            reader.Dispose();
        }
      }
    }

    /// <inheritdoc/>
    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

#endregion
  }
}

#endif
