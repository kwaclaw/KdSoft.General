using System.Threading;
using System.Threading.Tasks;

namespace KdSoft.StreamUtils
{
  /// <summary>
  /// Base interface for data readers.
  /// </summary>
  public interface IReader
  {
    /// <summary>
    /// Inidicates if the size of the data is known and returns it if true.
    /// </summary>
    /// <param name="size">Total size of data. Only valid if result is <c>true</c>.</param>
    /// <returns><c>true</c> if the data size is known; otherwise, <c>false</c>.</returns>
    /// <remarks>Implementation may decide to return the largest known size, if result is <c>false</c>.</remarks>
    bool GetSize(out long size);
  }

  /// <summary>
  /// Synchronous interface for reading sequentially from a data source.
  /// </summary>
  public interface ISerialReader : IReader
  {
    /// <summary>
    /// Reads up to <c>count</c> bytes sequentially from underlying entity (e.g. stream).
    /// </summary>
    /// <param name="buffer">Byte buffer to write the data into.</param>
    /// <param name="start">Position in buffer to start writing to.</param>
    /// <param name="count">Maximum number of bytes to read.</param>
    /// <returns><see cref="IOResult"/> instance.</returns>
    /// <remarks><list type="bullet">
    /// <item><description>If the returned count is less than requested this does not mean that the end of the data was reached - see <see cref="IOResult"/></description></item>
    /// </list></remarks>
    IOResult Read(byte[] buffer, int start, int count);
  }

  /// <summary>
  /// Asynchronous interface for reading sequentially from a data source.
  /// </summary>
  public interface ISerialAsyncReader : IReader
  {
    /// <summary>
    /// Reads up to <c>count</c> bytes sequentially from underlying entity (e.g. stream).
    /// </summary>
    /// <param name="buffer">Byte buffer to write the data into.</param>
    /// <param name="start">Position in buffer to start writing to.</param>
    /// <param name="count">Maximum number of bytes to read.</param>
    /// <param name="options">Task creation options to use.</param>
    /// <returns>Scheduled task instance that can be waited on, or <c>null</c> if reading is already complete (end of data encountered).
    /// Task result contains <see cref="IOResult"/> details.</returns>
    /// <remarks><list type="bullet">
    /// <item><description>If the returned count is less than requested this does not mean that the end of the data was reached - see <see cref="IOResult"/></description></item>
    /// </list></remarks>
    Task<IOResult> ReadAsync(byte[] buffer, int start, int count, TaskCreationOptions options);
  }

  /// <summary>
  /// Synchronous interface for reading randomly from a data source. Allows multiple reads of the same data.
  /// <remarks>Data must not change once read - the underlying entity is assumed to be constant while open.</remarks>
  /// </summary>
  public interface IRandomReader: IReader
  {
    /// <summary>
    /// Reads <c>count</c> bytes from underlying entity (e.g. stream) starting at position <c>offset</c>.
    /// </summary>
    /// <param name="buffer">Byte buffer to write the data into.</param>
    /// <param name="start">Position in buffer to start writing to.</param>
    /// <param name="count">Number of bytes to read.</param>
    /// <param name="sourceOffset">Position in underlying entity to start reading from.</param>
    /// <returns><see cref="IOResult"/> instance.</returns>
    /// <remarks><list type="bullet">
    /// <item><description>Implementation may limit the range of positions to read from, e.g. limited to XX bytes before the last
    /// sequentially read position. The returned offset and count can be affected - see <see cref="IOResult"/>.</description></item>
    /// <item><description>If the result has <c>Count == 0</c>, then the returned offset will indicate the nearest offset
    /// to the request offset that still contains data.</description></item>
    /// </list></remarks>
    IOResult Read(byte[] buffer, int start, int count, long sourceOffset);
  }

  /// <summary>
  /// Asynchronous interface for reading randomly from a data source. Allows multiple reads of the same data.
  /// <remarks>Data must not change once read - the underlying entity is assumed to be constant while open.</remarks>
  /// </summary>
  public interface IRandomAsyncReader: IReader
  {
    /// <summary>
    /// Reads <c>count</c> bytes from underlying entity (e.g. stream) starting at position <c>offset</c>.
    /// </summary>
    /// <param name="buffer">Byte buffer to write the data into.</param>
    /// <param name="start">Position in buffer to start writing to.</param>
    /// <param name="count">Number of bytes to read.</param>
    /// <param name="sourceOffset">Position in underlying entity to start reading from.</param>
    /// <param name="options">Task creation options to use.</param>
    /// <returns>Scheduled task instance that can be waited on (never <c>null</c>). Task result contains <see cref="IOResult"/> details.</returns>
    /// <remarks><list type="bullet">
    /// <item><description>Implementation may limit the range of positions to read from, e.g. limited to XX bytes before the last
    /// sequentially read position. The returned offset and count can be affected - see <see cref="IOResult"/>.</description></item>
    /// <item><description>If the result has <c>Count == 0</c>, then the returned offset will indicate the nearest offset
    /// to the request offset that still contains data.</description></item>
    /// </list></remarks>
    Task<IOResult> ReadAsync(byte[] buffer, int start, int count, long sourceOffset, TaskCreationOptions options);
  }
}
