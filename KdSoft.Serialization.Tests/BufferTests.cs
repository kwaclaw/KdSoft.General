using System;
using Xunit;

namespace KdSoft.Serialization.Tests
{
  public class BufferTests
  {
    decimal[] testDecimals;
    int[] testInts;

    public BufferTests() {
      this.testDecimals = new decimal[] { 122.456M, 305646.996e22M, -432567M, 7843561238.22e3M, -0.437243723M };
      this.testInts = new int[] { 55, 7438, -436843689, 296, -97653 };
    }

    [Fact]
    public void SerializeDecimalSpan() {
      var tmpArray = (decimal[])testDecimals.Clone();
      SpanHelpers.ReverseByteOrder(tmpArray);

      var tmpArray2 = (decimal[])testDecimals.Clone();
      for (int indx = 0; indx < tmpArray2.Length; indx++) {
        tmpArray2[indx] = tmpArray2[indx].ReverseByteOrder();
      }

      Assert.Equal<decimal[]>(tmpArray, tmpArray2);
    }

    [Fact]
    public void ReadWriteDecimals() {
      var buffer = new byte[1024];

      int writeIndex = 0;
      bool success = SpanHelpers.TryWriteBytes((ReadOnlySpan<decimal>)testDecimals, buffer, ref writeIndex);
      Assert.True(success);

      var bufferSlice = new ReadOnlySpan<byte>(buffer, 0, writeIndex);
      int readIndex = 0;
      var receiveArray = new decimal[testDecimals.Length];
      success = SpanHelpers.TryReadBytes(bufferSlice, ref readIndex, (Span<decimal>)receiveArray);
      Assert.True(success);
      Assert.Equal(writeIndex, readIndex);
      Assert.Equal<decimal[]>(testDecimals, receiveArray);
    }

    [Fact]
    public void ReverseIntSpanByteOrder() {
      var testInts2 = (int[])testInts.Clone();

      SpanHelpers.ReverseByteOrder(testInts2);
      // reverse back
      SpanHelpers.ReverseByteOrder(testInts2);

      Assert.Equal<int[]>(testInts, testInts2);
    }

    [Fact]
    public void ReverseDecimalSpanByteOrder() {
      var testDecimals2 = (decimal[])testDecimals.Clone();

      SpanHelpers.ReverseByteOrder(testDecimals2);
      // reverse back
      SpanHelpers.ReverseByteOrder(testDecimals2);

      Assert.Equal<decimal[]>(testDecimals, testDecimals2);
    }

    [Fact]
    public void ReadWriteDecimalsReverseByteOrder() {
      var buffer = new byte[1024];
      var byteConverter = new ByteConverter(reverse: true);

      int writeIndex = 0;
      byteConverter.WriteBytes((ReadOnlySpan<decimal>)testDecimals, buffer, ref writeIndex);

      var bufferSlice = new ReadOnlySpan<byte>(buffer, 0, writeIndex);
      int readIndex = 0;
      var receiveArray = new decimal[testDecimals.Length];
      byteConverter.ReadBytes(bufferSlice, ref readIndex, (Span<decimal>)receiveArray);
      Assert.Equal(writeIndex, readIndex);
      Assert.Equal<decimal[]>(testDecimals, receiveArray);
    }
  }
}
