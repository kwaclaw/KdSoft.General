using System;
using System.Diagnostics;

namespace KdSoft.StreamUtils
{
  /// <summary>
  /// Base class for push-style data transformations on data streams.
  /// </summary>
  /// <remarks>This implementation does not re-try data writes to the target if data could not be written,
  /// for instance due to network issues. It will simply accumulate unwritten output, potentially until
  /// the final input block is processed. This last call will then fail if not all output could be written.</remarks>
  public abstract class TransformFilterWriter: IFilterWriter
  {
    IFilterWriter outWriter;
    byte[] outBuffer;
    int outBufferOffset;
    bool complete;

    /// <summary>
    /// Constructor for <see cref="TransformFilterWriter"/>
    /// </summary>
    /// <param name="outWriter">Interface to write transformed data to target entity.</param>
    public TransformFilterWriter(IFilterWriter outWriter) {
      if (outWriter == null)
        throw new ArgumentNullException("outWriter");
      this.outWriter = outWriter;
      outBuffer = null;
      outBufferOffset = 0;
      complete = false;
    }

    /// <summary>
    /// Performs data transformation.
    /// </summary>
    /// <param name="inBuffer">Input buffer containing data to process.</param>
    /// <param name="inStart">Start position in input buffer.</param>
    /// <param name="inCount">Number of bytes in input buffer to process.</param>
    /// <param name="outBuffer">Output buffer to hold result of data processing. Can be re-allocated if size needs to be increased.
    ///   Can be set to point to <c>inBuffer</c> if input and output are identical and outStart was <c>0</c>.</param>
    /// <param name="outStart">Start position in output buffer to start writing processed data to.
    ///   It is possible that the output buffer could not be emptied in the last target write operation.</param>
    /// <returns>Number of bytes written to output buffer.</returns>
    protected abstract int Transform(byte[] inBuffer, int inStart, ref int inCount, ref byte[] outBuffer, int outStart);

    /// <summary>
    /// Performs data transformation of last data block.
    /// </summary>
    /// <param name="inBuffer">Input buffer containing data to process.</param>
    /// <param name="inStart">Start position in input buffer.</param>
    /// <param name="inCount">Number of bytes in input buffer to process.</param>
    /// <param name="outBuffer">Output buffer to hold result of data processing. Can be re-allocated if size needs to be increased.
    ///   Can be set to point to <c>inBuffer</c> if input and output are identical and outStart was <c>0</c>.</param>
    /// <param name="outStart">Start position in output buffer to start writing processed data to.
    ///   It is possible that the output buffer could not be emptied in the last target write operation.</param>
    /// <returns>Number of bytes written to output buffer.</returns>
    protected abstract int FinalTransform(byte[] inBuffer, int inStart, int inCount, ref byte[] outBuffer, int outStart);

    /// <summary>
    /// Processes data and writes transformed data to target entity.
    /// </summary>
    /// <param name="buffer">Input buffer containing data to process.</param>
    /// <param name="start">Start position in input buffer.</param>
    /// <param name="count">Number of bytes in input buffer to process.</param>
    /// <returns>Number of bytes from input buffer that could be processed. It is possible that not all input
    /// can be processed if for instance the transformation can only work on data blocks of a given size.</returns>
    public int Write(byte[] buffer, int start, int count) {
      if (complete)
        throw new InvalidOperationException("Transformation already complete.");
      byte[] targetBuffer = outBuffer;
      int generated = Transform(buffer, start, ref count, ref targetBuffer, outBufferOffset);
      if (object.ReferenceEquals(targetBuffer, buffer)) {
        Debug.Assert(outBufferOffset == 0, "Parameter outBufferOffset must be 0.");
        int written = outWriter.Write(buffer, start, count);
        int remaining = count - written;
        if (remaining > 0) {
          if (outBuffer == null || outBuffer.Length < remaining)
            outBuffer = new byte[((remaining >> 3) + 1) << 3];  // 8 byte aligned
          Buffer.BlockCopy(buffer, start + written, outBuffer, 0, remaining);
        }
        outBufferOffset = remaining;
      }
      else {
        outBuffer = targetBuffer;
        int writeCount = outBufferOffset + generated;
        int written = outWriter.Write(targetBuffer, 0, writeCount);
        int remaining = writeCount - written;
        if (remaining > 0)
          Buffer.BlockCopy(targetBuffer, written, targetBuffer, 0, remaining);
        outBufferOffset = remaining;
      }
      return count;
    }

    /// <summary>
    /// Process final block of input data and writes transformed data to target entity.
    /// </summary>
    /// <param name="buffer">Input buffer containing data to process.</param>
    /// <param name="start">Start position in input buffer.</param>
    /// <param name="count">Number of bytes in input buffer to process.</param>
    public void FinalWrite(byte[] buffer, int start, int count) {
      if (complete)
        throw new InvalidOperationException("Transformation already comlete.");
      byte[] targetBuffer = outBuffer;
      int generated = FinalTransform(buffer, start, count, ref targetBuffer, outBufferOffset);
      if (object.ReferenceEquals(targetBuffer, buffer)) {
        Debug.Assert(outBufferOffset == 0, "Parameter outBufferOffset must be 0.");
        outWriter.FinalWrite(buffer, start, count);
      }
      else {
        outWriter.FinalWrite(targetBuffer, 0, outBufferOffset + generated);
      }
      complete = true;
    }
  }
}
