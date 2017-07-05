using System;
using System.Security.Cryptography;

namespace KdSoft.StreamUtils
{
  /// <summary>
  /// <see cref="TransformFilterWriter"/> that encrypts a data stream.
  /// </summary>
  public class CryptoWriter: TransformFilterWriter
  {
    ICryptoTransform crypter;

    public CryptoWriter(IFilterWriter outWriter, ICryptoTransform crypter) : base(outWriter)
    {
      this.crypter = crypter;
    }

    // assumes that outBuffer has sufficient space
    int TransformSingleBlocks(byte[] inBuffer, int inStart, int blockCount, byte[] outBuffer, int outStart) {
      int firstOutStart = outStart;
      for (int indx = 0; indx < blockCount; indx++) {
        int written = crypter.TransformBlock(inBuffer, inStart, crypter.InputBlockSize, outBuffer, outStart);
        inStart += crypter.InputBlockSize;
        outStart += written;
      }
      return outStart - firstOutStart;
    }

    void CheckOutBuffer(ref byte[] outBuffer, int dataSize, int start) {
      int requiredOutSize = dataSize + start;
      if (outBuffer == null) {  // start must be 0
        outBuffer = new byte[((requiredOutSize >> 3) + 1) << 3];  // 8 byte aligned
      }
      else if (outBuffer.Length < requiredOutSize) {
        var oldBuffer = outBuffer;
        outBuffer = new byte[((requiredOutSize >> 3) + 1) << 3];  // 8 byte aligned
        if (start > 0)
          Buffer.BlockCopy(oldBuffer, 0, outBuffer, 0, start);
      }
    }

    protected override int Transform(byte[] inBuffer, int inStart, ref int inCount, ref byte[] outBuffer, int outStart) {
      int blockCount = inCount / crypter.InputBlockSize;
      inCount = blockCount * crypter.InputBlockSize;
      if (blockCount == 0)
        return 0;
      CheckOutBuffer(ref outBuffer, blockCount * crypter.OutputBlockSize, outStart);
      int totalOutput = 0;
      if (crypter.CanTransformMultipleBlocks)
        totalOutput = crypter.TransformBlock(inBuffer, inStart, inCount, outBuffer, outStart);
      else
        totalOutput = TransformSingleBlocks(inBuffer, inStart, blockCount, outBuffer, outStart);
      return totalOutput;
    }

    protected override int FinalTransform(byte[] inBuffer, int inStart, int inCount, ref byte[] outBuffer, int outStart) {
      int blockCount = inCount / crypter.InputBlockSize;
      // allocate enough output buffer for the final block
      CheckOutBuffer(ref outBuffer, (blockCount + 1) * crypter.OutputBlockSize, outStart);
      int blockedInCount = blockCount * crypter.InputBlockSize;
      int totalOutput = 0;
      if (blockCount > 0) {
        if (crypter.CanTransformMultipleBlocks)
          totalOutput = crypter.TransformBlock(inBuffer, inStart, blockedInCount, outBuffer, outStart);
        else
          totalOutput = TransformSingleBlocks(inBuffer, inStart, blockCount, outBuffer, outStart);
        inStart += blockedInCount;
        inCount -= blockedInCount;
        outStart += totalOutput;
      }
      byte[] finalBuffer = crypter.TransformFinalBlock(inBuffer, inStart, inCount);
      Buffer.BlockCopy(finalBuffer, 0, outBuffer, outStart, finalBuffer.Length);
      return totalOutput + finalBuffer.Length;
    }

    public ICryptoTransform CryptoTransform {
      get { return crypter; }
    } 
  }
}
