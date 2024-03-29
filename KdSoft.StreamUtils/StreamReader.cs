﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KdSoft.StreamUtils
{
  /// <summary>
  /// Base class for stream reader implementations.
  /// </summary>
  /// <typeparam name="T">Stream type.</typeparam>
  public class StreamReader<T> where T : Stream
  {
    protected readonly T stream_;
    protected readonly object syncObj = new object();

    public T Stream {
      get { return stream_; }
    }

    public StreamReader(T stream) {
      if (!stream.CanRead)
        throw new ArgumentException("Stream must support reading.", "stream");
      this.stream_ = stream;
    }

    public void Close() {
      lock (syncObj) {
        stream_.Dispose();
      }
    }
  }

  /// <summary>
  /// Wrapper for <see cref="Stream"/> that exposes an <see cref="ISerialReader"/> interface.
  /// </summary>
  /// <typeparam name="T">Stream type.</typeparam>
  /// <remarks>><list type="bullet">
  /// <item><description>The correctness of operations relies on the stream position not being modified
  /// outside of this <see cref="SerialStreamReader{T}"/> instance.</description></item>
  /// </list></remarks>
  public class SerialStreamReader<T>: StreamReader<T>, ISerialReader where T : Stream
  {
    protected bool endEncountered;

    public SerialStreamReader(T stream) : base(stream) {
      endEncountered = false;
    }

    #region IReader

    public bool GetSize(out long size) {
      lock (syncObj) {
        size = stream_.Position;
        return endEncountered;
      }
    }

    #endregion

    #region ISerialReader Members

    public IOResult Read(byte[] buffer, int start, int count) {
      lock (syncObj) {
        var offset = stream_.Position;
        var readCount = stream_.Read(buffer, start, count);
        bool isEnd = readCount == 0;
        if (isEnd) {
          endEncountered = true;
        }
        return new IOResult(offset, readCount, isEnd);
      }
    }

    #endregion
  }

  /// <summary>
  /// Wrapper for <see cref="Stream"/> that exposes an <see cref="ISerialAsyncReader"/> interface.
  /// </summary>
  /// <typeparam name="T">Stream type.</typeparam>
  /// <remarks>><list type="bullet">
  /// <item><description>The correctness of operations relies on the stream position not being modified
  /// outside of this <see cref="SerialAsyncStreamReader{T}"/> instance.</description></item>
  /// </list></remarks>
  public class SerialAsyncStreamReader<T>: StreamReader<T>, ISerialAsyncReader where T : Stream
  {
    protected bool endEncountered;

    public SerialAsyncStreamReader(T stream) : base(stream) {
      endEncountered = false;
    }

    #region IReader

    public bool GetSize(out long size) {
      lock (syncObj) {
        size = stream_.Position;
        return endEncountered;
      }
    }

    #endregion

    #region ISerialAsyncReader Members

    public async Task<IOResult> ReadAsync(byte[] buffer, int start, int count, TaskCreationOptions options) {
      bool lockWasTaken = false;
      try {
        Monitor.Enter(syncObj, ref lockWasTaken);
        long position = stream_.Position;
        int readCount = await stream_.ReadAsync(buffer, start, count).ConfigureAwait(false);
        bool isEnd = readCount == 0;
        if (isEnd) {
          endEncountered = true;
        }
        return new IOResult(position, readCount, isEnd);
      }
      finally {
        if (lockWasTaken)
          Monitor.Exit(syncObj);
      }

      #endregion
    }
  }

  /// <summary>
  /// Wrapper for <see cref="Stream"/> that exposes an <see cref="IRandomReader"/> interface.
  /// </summary>
  /// <typeparam name="T">Stream type.</typeparam>
  /// <remarks>><list type="bullet">
  /// <item><description>This implementation  modifies the stream position temporarily under lock protection and resets it at the end of each call,
  /// so that it can be used concurrently with a <see cref="SerialStreamReader{T}"/> instance on the same underlying stream.</description></item>
  /// </list></remarks>
  public class RandomStreamReader<T>: StreamReader<T>, IRandomReader where T : Stream
  {
    public RandomStreamReader(T stream) : base(stream) {
      if (!stream.CanSeek)
        throw new ArgumentException("Stream must support seeking.", "stream");
    }

    #region IReader

    public bool GetSize(out long size) {
      lock (syncObj) {
        size = stream_.Length;
        return true;
      }
    }

    #endregion

    #region IRandomReader Members

    public IOResult Read(byte[] buffer, int start, int count, long sourceOffset) {
      long slen = stream_.Length;
      // since it is legal to set the stream position beyond the end of the stream, we have to check
      long readOffset = sourceOffset >= slen ? slen : sourceOffset;
      lock (syncObj) {
        long position = stream_.Position;
        try {
          stream_.Position = readOffset;
          var readCount = stream_.Read(buffer, start, count);
          return new IOResult(readOffset, readCount, readOffset + readCount >= slen);
        }
        finally {  // reset position so that sequential reads don't get messed up
          stream_.Position = position;
        }
      }
    }

    #endregion
  }

  /// <summary>
  /// Wrapper for <see cref="Stream"/> that exposes an <see cref="IRandomAsyncReader"/> interface.
  /// </summary>
  /// <typeparam name="T">Stream type.</typeparam>
  /// <remarks>><list type="bullet">
  /// <item><description>This implementation  modifies the stream position temporarily under lock protection and resets it at the end of each call,
  /// so that it can be used concurrently with a <see cref="SerialAsyncStreamReader{T}"/> instance on the same underlying stream.</description></item>
  /// </list></remarks>
  public class RandomAsyncStreamReader<T>: StreamReader<T>, IRandomAsyncReader where T : Stream
  {
    public RandomAsyncStreamReader(T stream) : base(stream) {
      if (!stream.CanSeek)
        throw new ArgumentException("Stream must support seeking.", "stream");
    }

    #region IReader

    public bool GetSize(out long size) {
      lock (syncObj) {
        size = stream_.Length;
        return true;
      }
    }

    #endregion

    #region IRandomAsyncReader Members

    public async Task<IOResult> ReadAsync(byte[] buffer, int start, int count, long sourceOffset, TaskCreationOptions options) {
      long slen = stream_.Length;
      // since it is legal to set the stream position beyond the end of the stream, we have to check
      long readOffset = sourceOffset >= slen ? slen : sourceOffset;
      bool lockWasTaken = false;
      try {
        Monitor.Enter(syncObj, ref lockWasTaken);
        long position = stream_.Position;
        try {
          stream_.Position = readOffset;
          int readCount = await stream_.ReadAsync(buffer, start, count).ConfigureAwait(false);
          bool isEnd = readOffset + readCount >= stream_.Length;
          return new IOResult(readOffset, readCount, isEnd);
        }
        finally {  // reset position so that sequential reads don't get messed up
          stream_.Position = position;
        }
      }
      finally {
        if (lockWasTaken)
          Monitor.Exit(syncObj);
      }
    }

    #endregion
  }
}
