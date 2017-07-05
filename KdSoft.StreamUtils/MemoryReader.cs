using System;
using System.Threading.Tasks;

namespace KdSoft.StreamUtils
{
  public class MemoryReader: ISerialReader, IRandomAsyncReader
  {
    byte[] bytes;
    int position;

    public MemoryReader(byte[] bytes) {
      if (bytes == null)
        throw new ArgumentNullException("bytes");
      this.bytes = bytes;
      position = 0;
    }

    #region ISerialReader Members

    public IOResult Read(byte[] buffer, int start, int count) {
      int delta = position + count - bytes.Length;
      bool isEnd = delta >= 0;
      int readCount = isEnd ? (int)(count - delta) : count;
      Buffer.BlockCopy(bytes, position, buffer, start, readCount);
      var result = new IOResult(position, readCount, isEnd);
      position += readCount;
      return result;
    }

    #endregion

    #region IReader Members

    public bool GetSize(out long size) {
      size = bytes.Length;
      return true;
    }

    #endregion

    #region IRandomReader Members

    public IOResult Read(byte[] buffer, int start, int count, long sourceOffset) {
      long delta = sourceOffset - bytes.Length;
      if (delta >= 0)
        return new IOResult(bytes.Length, 0, true);
      delta += count;
      bool isEnd = delta >= 0;
      int readCount = isEnd ? (int)(count - delta) : count;
      Buffer.BlockCopy(bytes, (int)sourceOffset, buffer, start, readCount);
      return new IOResult(sourceOffset, readCount, isEnd);
    }

    #endregion

    #region IRandomAsyncReader Members

    public Task<IOResult> ReadAsync(byte[] buffer, int start, int count, long sourceOffset, TaskCreationOptions options) {
      var tcs = new TaskCompletionSource<IOResult>(options);
      try {
        var ioResult = Read(buffer, start, count, sourceOffset);
        tcs.SetResult(ioResult);
      }
      catch (Exception ex) {
        tcs.SetException(ex);
      }
      return tcs.Task;
    }

    #endregion
  }
}
