﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KdSoft.Utils;

namespace KdSoft.StreamUtils
{
  /// <summary>
  /// Base class for stream writer implementations.
  /// </summary>
  /// <typeparam name="T">Stream type.</typeparam>
  public class StreamWriter<T> where T : Stream
  {
    protected readonly T stream;

    public T Stream {
      get { return stream; }
    }

    public StreamWriter(T stream) {
      if (!stream.CanWrite)
        throw new ArgumentException("Stream must support writing.", "stream");
      this.stream = stream;
    }

    // requires lock on stream
    protected virtual void Abort() {
      stream.SetLength(0);
    }

    public void Close(bool abort = false) {
      lock (stream) {
        try {
          if (abort)
            Abort();
          else
            stream.Flush();
        }
        finally {
          stream.Dispose();
        }
      }
    }
  }

  /// <summary>
  /// Wrapper for <see cref="Stream"/> that exposes an <see cref="ISerialWriter"/> interface.
  /// </summary>
  /// <typeparam name="T">Stream type.</typeparam>
  /// <remarks>><list type="bullet">
  /// <item>The correctness of operations relies on the stream position not being modified
  /// outside of this <see cref="SerialStreamWriter{T}"/> instance.</item>
  /// </list></remarks>
  public class SerialStreamWriter<T> : StreamWriter<T>, ISerialWriter where T : Stream
  {
    protected bool endEncountered;

    public SerialStreamWriter(T stream): base(stream) {
      endEncountered = false;
    }

    #region ISerialWriter Members

    public IOResult Write(byte[] buffer, int start, int count) {
      lock (stream) {
        if (endEncountered) {
          return new IOResult(-1, 0, true);
        }
        var ioResult = new IOResult(stream.Position, count, false);
        stream.Write(buffer, start, count);
        return ioResult;
      }
    }

    public long FinalWrite(byte[] buffer, int start, int count) {
      lock (stream) {
        if (endEncountered)
          return -1;
        long endPos = stream.Position + count;
        stream.Write(buffer, start, count);
        endEncountered = true;
        return endPos;
      }
    }

    #endregion
  }

  /// <summary>
  /// Wrapper for <see cref="Stream"/> that exposes an <see cref="ISerialAsyncWriter"/> interface.
  /// </summary>
  /// <typeparam name="T">Stream type.</typeparam>
  /// <remarks>><list type="bullet">
  /// <item>The correctness of operations relies on the stream position not being modified
  /// outside of this <see cref="SerialAsyncStreamWriter{T}"/> instance.</item>
  /// </list></remarks>
  public class SerialAsyncStreamWriter<T> : StreamWriter<T>, ISerialAsyncWriter where T : Stream
  {
    protected bool endEncountered;

    public SerialAsyncStreamWriter(T stream): base(stream) {
      endEncountered = false;
    }

    #region ISerialAsyncWriter Members

    async Task<IOResult> InternalWriteAsync(byte[] buffer, int start, int count) {
      bool lockWasTaken = false;
      try {
        Monitor.Enter(stream, ref lockWasTaken);
        var ioResult = new IOResult(stream.Position, count, false);
        await stream.WriteAsync(buffer, start, count).ConfigureAwait(false);
        return ioResult;
      }
      finally {
        if (lockWasTaken) Monitor.Exit(stream);
      }
    }

    public Task<IOResult> WriteAsync(byte[] buffer, int start, int count, TaskCreationOptions options) {
      lock(stream) {
        if (endEncountered) {
          return null;
        }
        return InternalWriteAsync(buffer, start, count);
      }
    }

    async Task<long> InternalFinalWriteAsync(byte[] buffer, int start, int count) {
      bool lockWasTaken = false;
      try {
        Monitor.Enter(stream, ref lockWasTaken);
        long endPos = stream.Position + count;
        await stream.WriteAsync(buffer, start, count).ConfigureAwait(false);
        endEncountered = true;
        return endPos;
      }
      finally {
        if (lockWasTaken) Monitor.Exit(stream);
      }
    }

    public Task<long> FinalWriteAsync(byte[] buffer, int start, int count, TaskCreationOptions options) {
      lock(stream) {
        if (endEncountered) {
          return null;
        }
        return InternalFinalWriteAsync(buffer, start, count);
      }
    }

    #endregion
  }

  /// <summary>
  /// Wrapper for <see cref="Stream"/> that exposes an <see cref="IRandomWriter"/> interface.
  /// </summary>
  /// <typeparam name="T">Stream type.</typeparam>
  /// <remarks>><list type="bullet">
  /// <item>This implementation  modifies the stream position temporarily under lock protection and resets it at the end of each call,
  /// so that it can be used concurrently with a <see cref="SerialStreamWriter{T}"/> instance on the same underlying stream.</item>
  /// <item>This implementations allows multiple writes to the same range in the target stream.</item>
  /// </list></remarks>
  public class RandomStreamWriter<T> : StreamWriter<T>, IRandomWriter where T : Stream 
  {
    protected bool endEncountered;
    protected bool isComplete;
    long length;

    public RandomStreamWriter(T stream): base(stream) {
      if (!stream.CanSeek)
        throw new ArgumentException("Stream must support seeking.", "stream");
      endEncountered = false;
      length = 0;
    }

    /// <summary>
    /// Called when writing has been completed successfully.
    /// </summary>
    public event EventHandler OnCompleted;

    void HandleCompleted() {
      var onCompleted = OnCompleted;
      if (onCompleted != null)
        onCompleted(this, EventArgs.Empty);
    }

    #region IRandomWriter Members

    // must be called under protection of lock
    void InternalWrite(byte[] buffer, int start, int count, long targetOffset) {
      long position = stream.Position;
      try {
        stream.Seek(targetOffset, SeekOrigin.Begin);
        stream.Write(buffer, start, count);
      }
      finally {  // reset position to keep serial writes correct
        stream.Position = position;
      }
    }

    public bool Write(byte[] buffer, int start, int count, long targetOffset) {
      lock (stream) {
        if (isComplete)
          return false;
        if (endEncountered && (targetOffset + count) > length)
          throw new InvalidOperationException("Writable range of data stream exceeded.");
        InternalWrite(buffer, start, count, targetOffset);
      }
      return true;
    }

    public bool EndWrite(byte[] buffer, int start, int count, long targetOffset) {
      lock (stream) {
        if (isComplete || endEncountered) {
          return false;
        }
        long targetSize = targetOffset + count;
        if (stream.Length > targetSize)
          throw new InvalidOperationException("Data stream already exceeds end position.");
        InternalWrite(buffer, start, count, targetOffset);
        endEncountered = true;
        length = targetSize;
      }
      return true;
    }

    public bool SetComplete(bool abort) {
      lock (stream) {
        if (isComplete)
          return false;
        if (!abort && !endEncountered)
          throw new InvalidOperationException("Cannot set complete before EndWrite was called.");
        isComplete = true;
      }
      HandleCompleted();
      return true;
    }

    #endregion

    public bool GetSize(out long size) {
      lock (stream) {
        if (endEncountered)
          size = length;
        else
          size = stream.Position;
        return endEncountered;
      }
    }
  }

  /// <summary>
  /// Wrapper for <see cref="Stream"/> that exposes an <see cref="IRandomAsyncWriter"/> interface.
  /// </summary>
  /// <typeparam name="T">Stream type.</typeparam>
  /// <remarks>><list type="bullet">
  /// <item>This implementation  modifies the stream position temporarily under lock protection and resets it at the end of each call,
  /// so that it can be used concurrently with a <see cref="SerialAsyncStreamWriter{T}"/> instance on the same underlying stream.</item>
  /// <item>This implementations allows multiple writes to the same range in the target stream.</item>
  /// </list></remarks>
  public class RandomAsyncStreamWriter<T> : StreamWriter<T>, IRandomAsyncWriter, IDisposable where T : Stream
  {
    protected bool endEncountered;
    protected bool setComplete;
    readonly TaskWaiter completeWaiter;
    long length;

    public RandomAsyncStreamWriter(T stream): base(stream) {
      if (!stream.CanSeek)
        throw new ArgumentException("Stream must support seeking.", "stream");
      completeWaiter = new TaskWaiter(true);
    }

    #region IRandomAsyncWriter Members

    Task InternalWriteAsync(byte[] buffer, int start, int count, long targetOffset) {
      long position = stream.Position;
      try {
        stream.Seek(targetOffset, SeekOrigin.Begin);
        var task = stream.WriteAsync(buffer, start, count);
        completeWaiter.Add(task);
        return task;
      }
      finally {  // reset position to keep serial writes correct
        stream.Position = position;
      }
    }

    public Task WriteAsync(byte[] buffer, int start, int count, long targetOffset, TaskCreationOptions options) {
      Task task;
      lock (stream) {
        if (setComplete)
          return null;
        if (endEncountered && (targetOffset + count) > length)
          throw new InvalidOperationException("Writable range of data stream exceeded.");
        task = InternalWriteAsync(buffer, start, count, targetOffset);
      }
      return task;
    }

    public Task EndWriteAsync(byte[] buffer, int start, int count, long targetOffset, TaskCreationOptions options) {
      Task task;
      lock (stream) {
        if (setComplete || endEncountered)
          return null;
        long targetSize = targetOffset + count;
        if (stream.Length > targetSize)
          throw new InvalidOperationException("Data stream already exceeds end position.");
        task = InternalWriteAsync(buffer, start, count, targetOffset);
        endEncountered = true;
        length = targetSize;
      }
      return task;
    }

    public Task SetComplete(bool abort, TaskCreationOptions options) {
      lock (stream) {
        if (setComplete)
          return null;
        if (!abort && !endEncountered)
          throw new InvalidOperationException("Cannot set complete before EndWrite was called.");
        setComplete = true;
      }
      var tcs = new TaskCompletionSource<object>(options);
      completeWaiter.OnCompleted += (s, e) => {
        if (e.Value == null)
          tcs.TrySetResult(null);
        else
          tcs.TrySetException(e.Value);
      };
      completeWaiter.CompleteAdding();
      return tcs.Task;
    }

    #endregion

    public bool GetSize(out long size) {
      lock (stream) {
        if (endEncountered)
          size = length;
        else
          size = stream.Position;
        return endEncountered;
      }
    }

    #region IDisposable Members

    protected virtual void Dispose(bool disposing) {
      if (disposing) {
        var cw = completeWaiter;
        if (cw != null) {
          cw.Dispose();
        }
      }
    }

    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    #endregion
  }

  /// <summary>
  /// Convenience class wrapping Stream to implement <see cref="IFilterWriter"/> interface.
  /// </summary>
  /// <typeparam name="T">Stream type.</typeparam>
  /// <remarks>Not thread-safe.</remarks>
  public class FilterStreamWriter<T>: IFilterWriter where T: Stream
  {
    T stream;

    protected T Stream {
      get { return stream; }
    }

    public FilterStreamWriter(T stream) {
      if (stream == null)
        throw new ArgumentNullException("stream");
      this.stream = stream;
    }

    #region IFilterWriter Members

    public int Write(byte[] buffer, int start, int count) {
      if (stream == null)
        throw new InvalidOperationException("No more writes allowed.");
      stream.Write(buffer, start, count);
      return count;
    }

    public void FinalWrite(byte[] buffer, int start, int count) {
      if (stream == null)
        throw new InvalidOperationException("No more writes allowed.");
      stream.Write(buffer, start, count);
      stream = null;
    }

    #endregion
  }
}

