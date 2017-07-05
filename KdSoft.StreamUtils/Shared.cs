
namespace KdSoft.StreamUtils
{
  public static class Constants
  {
    public const int DefaultIOConcurrency = 8;
    public const int DefaultBufferSize = 16384;
  }

  /// <summary>
  /// Result returned from an IO operation.
  /// </summary>
  public struct IOResult
  {
    long offset;
    int count;
    bool isEnd;

    public IOResult(long offset, int count, bool isEnd) {
      this.offset = offset;
      this.count = count;
      this.isEnd = isEnd;
    }

    /// <summary>
    /// Offset from starting position at which the IO operation was started.
    /// If the value is <c>&lt; 0</c> then the operation was not performed, the reason depends on context:
    /// for instance, a reason could be that a write was requested after writing was already completed.</c>
    /// If the value of <see cref="Count"/> is <c>0</c>, then the Offset may be meaningless, depending on
    /// the implementation it is used in. In some cases it can indicate the closest available data offset.
    /// </summary>
    public long Offset { get { return offset; } }

    /// <summary>
    /// Number of bytes processed, read or written.
    /// </summary>
    public int Count { get { return count; } }

    /// <summary>
    /// Indicating if this offset and count represents the end of the data.
    /// </summary>
    /// <value><c>true</c> if <c>offset+count</c> is at the end; otherwise, <c>false</c>.</value>
    public bool IsEnd { get { return isEnd; } }
  }
}
