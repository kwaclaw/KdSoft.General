#if NETSTANDARD2_0

using System;
using System.Security.Cryptography;

namespace KdSoft.StreamUtils
{
  /// <summary>
  /// <see cref="TransformFilterWriter"/> that calculates the hash value of a data stream while passing the data through unchanged.
  /// </summary>
  public class HashWriter: TransformFilterWriter
  {
    HashAlgorithm hasher;

    public HashWriter(IFilterWriter outWriter, HashAlgorithm hasher) : base(outWriter) {
      this.hasher = hasher;
    }

    protected override int Transform(byte[] inBuffer, int inStart, ref int inCount, ref byte[] outBuffer, int outStart) {
      // assumption is we always process all input, so we ignore return value of TransformBlock
      hasher.TransformBlock(inBuffer, inStart, inCount, null, 0);
      if (outStart == 0) {
        outBuffer = inBuffer;
        return inCount;
      }
      int requiredOutSize = outStart + inCount;
      if (outBuffer.Length < requiredOutSize) {
        var oldBuffer = outBuffer;
        outBuffer = new byte[((requiredOutSize >> 3) + 1) << 3];  // 8 byte aligned
        Buffer.BlockCopy(oldBuffer, 0, outBuffer, 0, outStart);
      }
      Buffer.BlockCopy(inBuffer, inStart, outBuffer, outStart, inCount);
      return inCount;
    }

    protected override int FinalTransform(byte[] inBuffer, int inStart, int inCount, ref byte[] outBuffer, int outStart) {
      // optimization - we don't need an array copy returned from TransformFinalBlock, so we call Transform first
      Transform(inBuffer, inStart, ref inCount, ref outBuffer, outStart);
      hasher.TransformFinalBlock(inBuffer, inStart + inCount, 0);  // passing 0 for count avoids array copy
      return inCount;
    }

    public HashAlgorithm HashAlgorithm {
      get { return hasher; }
    }
  }
}

#endif
