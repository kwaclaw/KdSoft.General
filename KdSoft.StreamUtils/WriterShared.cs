using System.Threading.Tasks;

namespace KdSoft.StreamUtils
{
  /// <summary>
  /// Base interface for writers.
  /// </summary>
  public interface IWriter
  {
    // Base IWriter interface has no members
  }

  /// <summary>
  /// Synchronous interface for writing data, to be used for stream-like push processing, transforming or filtering.
  /// </summary>
  public interface IFilterWriter: IWriter
  {
    /// <summary>
    /// Write operation, to be called at start and in middle of processing.
    /// </summary>
    /// <param name="buffer">Byte buffer containing data to be written.</param>
    /// <param name="start">Start index in buffer of data to write.</param>
    /// <param name="count">Number of bytes to write.</param>
    /// <returns>Number of bytes written. Can be less than requested, including <c>0</c>.</returns>
    /// <remarks>Once cannot assume that the remaining data that could not be written
    /// will be sucessfully processed on a second call. They could be insufficient
    /// for processing in one call and will need to be combined with more data.</remarks>
    int Write(byte[] buffer, int start, int count);

    /// <summary>
    /// Write operation, to be called at end of processing.
    /// </summary>
    /// <param name="buffer">Byte buffer containing data to be written.</param>
    /// <param name="start">Start index in buffer of data to write.</param>
    /// <param name="count">Number of bytes to write.</param>
    /// <remarks>After this call no more write operations are allowed.
    /// If not all data could be written, this call will throw an exception.</remarks>
    void FinalWrite(byte[] buffer, int start, int count);
  }

  /// <summary>
  /// Synchronous interface for writing sequentially to a data sink.
  /// </summary>
  public interface ISerialWriter : IWriter
  {
    /// <summary>
    /// Write operation, to be called at start and in middle of processing.
    /// </summary>
    /// <param name="buffer">Byte buffer containing data to be written.</param>
    /// <param name="start">Start index in buffer of data to write.</param>
    /// <param name="count">Number of bytes to write.</param>
    /// <returns>Number of bytes written and offset. Can be less than requested. <see cref="IOResult"/>.</returns>
    /// <remarks>Once cannot assume that the remaining data that could not be written
    /// will be sucessfully processed on a second call. They could be insufficient
    /// for processing in one call and will need to be combined with more data.</remarks>
    IOResult Write(byte[] buffer, int start, int count);

    /// <summary>
    /// Write operation, to be called at end of processing.
    /// </summary>
    /// <param name="buffer">Byte buffer containing data to be written.</param>
    /// <param name="start">Start index in buffer of data to write.</param>
    /// <param name="count">Number of bytes to write.</param>
    /// <returns>Final size of data entity.</returns>
    /// <remarks>After this call no more write operations are allowed.
    /// If not all data could be written, this call will throw an exception.</remarks>
    long FinalWrite(byte[] buffer, int start, int count);
  }

  /// <summary>
  /// Asynchronous interface for writing sequentially to a data sink.
  /// </summary>
  public interface ISerialAsyncWriter : IWriter
  {
    /// <summary>
    /// Writes buffer to the underlying data sink sequentially.
    /// </summary>
    /// <param name="buffer">Byte buffer to write the data from.</param>
    /// <param name="start">Position in buffer to start reading from.</param>
    /// <param name="count">Number of bytes to write.</param>
    /// <param name="options">Task creation options to use.</param>
    /// <returns>Scheduled task instance that can be waited on, or <c>null</c> if in a completed state - usually if
    /// <see cref="FinalWriteAsync"/>  was  called already. Task result contains <see cref="IOResult"/> details.</returns>
    /// <remarks><list type="bullet">
    /// <item><description>Implementation may limit write capacity, the returned count can be affected - see <see cref="IOResult"/>.</description></item>
    /// </list></remarks>
    Task<IOResult> WriteAsync(byte[] buffer, int start, int count, TaskCreationOptions options);

    /// <summary>
    /// Writes final buffer to the underlying data sink sequentially. Moves writer to completed state.
    /// </summary>
    /// <param name="buffer">Byte buffer to write the data from.</param>
    /// <param name="start">Position in buffer to start reading from.</param>
    /// <param name="count">Number of bytes to write.</param>
    /// <param name="options">Task creation options to use.</param>
    /// <returns>Scheduled task instance that can be waited on, or <c>null</c> if in a completed state.
    /// Task result contains final size of data.</returns>
    /// <remarks>Will throw exception if not all input data could be processed.</remarks>
    Task<long> FinalWriteAsync(byte[] buffer, int start, int count, TaskCreationOptions options);
  }

  /// <summary>
  /// Synchronous interface for writing randomly to a data sink.
  /// </summary>
  /// <remarks>Intended to completely fill the underlying data entity without gaps. The implementaion may,
  /// but does not have to, ignore parts of a write operation that would overlap with already written data.</remarks>
  public interface IRandomWriter : IWriter
  {
    /// <summary>
    /// Writes buffer to the underlying data sink at a given offset.
    /// </summary>
    /// <param name="buffer">Byte buffer to read the input data from.</param>
    /// <param name="start">Position in buffer to start reading input from.</param>
    /// <param name="count">Number of bytes to write to target entity.</param>
    /// <param name="targetOffset">Position in underlying entity to start writing to.</param>
    /// <returns><c>true</c> if call was successful, <c>false</c> if call was redundant.</returns>
    /// <remarks><list type="bullet">
    /// <item><description>Writing beyond the end boundaries of the writable range may result in error, this is left to the implementation.
    /// The end of the range is only known after <see cref="EndWrite"/> was called.</description></item>
    /// <item><description>The implementation may decide to consider the data complete once all data in the range was written at least once,
    /// and then do nothing for subsequent write requests.</description></item>
    /// </list></remarks>
    bool Write(byte[] buffer, int start, int count, long targetOffset);

    /// <summary>
    /// Writes end buffer to the underlying data sink, thus determining the final size of the target data stream.
    /// </summary>
    /// <param name="buffer">Byte buffer to write the data from.</param>
    /// <param name="start">Position in buffer to start reading from.</param>
    /// <param name="count">Number of bytes to write.</param>
    /// <param name="targetOffset">Position in underlying entity to start writing to.</param>
    /// <returns><c>true</c> if call was successful, <c>false</c> if already in a completed state.</returns>
    /// <remarks><list type="bullet">
    /// <item><description>This can be called at any time, that is, even before other writes have been performed.</description></item>
    /// <item><description>If the data stream already extends beyond the end position (targetOffset + count) then
    /// the implementation may, but does not have to, throw an exception. If it does not throw, then
    /// the data stream will be truncated at the end position.</description></item>
    /// <item><description>If the completed state was reached by calling <see cref="ISerialWriter.FinalWrite"/>  on the same
    /// underlying entity, then this may do nothing  - and return <c>false</c> - even on the first call.</description></item>
    /// </list></remarks>
    bool EndWrite(byte[] buffer, int start, int count, long targetOffset);

    /// <summary>
    /// Indicates that no more writes will be requested, and that the underlying entity can be processed.
    /// Will throw exception if <see cref="EndWrite"/> has not been called yet, unless the abort flag is passed.
    /// </summary>
    /// <param name="abort">Indicates that writing will be stopped, even if the stream is not in a valid state.</param>
    /// <returns><c>true</c> if successful, <c>false</c> if already complete.</returns>
    bool SetComplete(bool abort);
  }

  /// <summary>
  /// Asynchronous interface for writing randomly to a data sink.
  /// </summary>
  /// <remarks>Intended to completely fill the underlying data entity without gaps. The implementaion may,
  /// but does not have to, ignore parts of a write operation that would overlap with already written data.</remarks>
  public interface IRandomAsyncWriter : IWriter
  {
    /// <summary>
    /// Writes buffer to the underlying data sink at a given offset.
    /// </summary>
    /// <param name="buffer">Byte buffer to write the data from.</param>
    /// <param name="start">Position in buffer to start reading from.</param>
    /// <param name="count">Number of bytes to write.</param>
    /// <param name="targetOffset">Position in underlying entity to start writing to.</param>
    /// <param name="options">Task creation options to use.</param>
    /// <returns>Scheduled task instance that can be waited on, or <c>null</c> if in a completed state.</returns>
    /// <remarks><list type="bullet">
    /// <item><description>Writing beyond the end boundaries of the writable range may result in error, this is left to the implementation.
    /// The end of the range is only known after <see cref="EndWriteAsync"/> was called.</description></item>
    /// <item><description>The implementation may decide to consider the data complete once all data in the range was written at least once,
    /// and then return <c>null</c> for subsequent write requests.</description></item>
    /// </list></remarks>
    Task WriteAsync(byte[] buffer, int start, int count, long targetOffset, TaskCreationOptions options);

    /// <summary>
    /// Writes end buffer to the underlying data sink, thus determining the final size of the target data stream.
    /// </summary>
    /// <param name="buffer">Byte buffer to write the data from.</param>
    /// <param name="start">Position in buffer to start reading from.</param>
    /// <param name="count">Number of bytes to write.</param>
    /// <param name="targetOffset">Position in underlying entity to start writing to.</param>
    /// <param name="options">Task creation options to use.</param>
    /// <returns>Scheduled task instance that can be waited on, or <c>null</c> if in a completed state.</returns>
    /// <remarks><list type="bullet">
    /// <item><description>This can be called at any time, that is, even before other writes have been performed.</description></item>
    /// <item><description>If the data stream already extends beyond the end position (targetOffset + count) then
    /// the implementation may, but does not have to, throw an exception. If it does not throw, then
    /// the data stream will be truncated at the end position.</description></item>
    /// <item><description>If the completed state was reached by calling <see cref="ISerialAsyncWriter.FinalWriteAsync"/>  on the same
    /// underlying entity, then this may return <c>null</c> even on the first call.</description></item>
    /// </list></remarks>
    Task EndWriteAsync(byte[] buffer, int start, int count, long targetOffset, TaskCreationOptions options);

    /// <summary>
    /// Indicates that no more writes will be requested, and that the underlying entity can be processed.
    /// Will throw exception if <see cref="EndWriteAsync"/> has not been called yet, unless the abort flag is passed.
    /// </summary>
    /// <param name="abort">Indicates that writing will be stopped, even if the stream is not in a valid state.</param>
    /// <param name="options">Task creation options to use.</param>
    /// <returns>Task instance that can be waited on, or <c>null</c> if in a completed state.
    /// The task completes once all write tasks have finished.</returns>
    Task SetComplete(bool abort, TaskCreationOptions options);
  }
}
