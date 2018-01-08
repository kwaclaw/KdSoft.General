using System;
using KdSoft.Serialization.Buffer;
using Xunit;

namespace KdSoft.Serialization.Tests
{
  public class BufferTests
  {
    [Fact]
    public void SerializeDecimalSpan() {
      var testArray = new decimal[] { 122.456M, 305646.996e22M, -432567M, 7843561238.22e3M, -0.437243723M };

      var tmpArray = (decimal[])testArray.Clone();
      BufferHelpers.ReverseByteOrder(tmpArray);

      var tmpArray2 = (decimal[])testArray.Clone();
      for (int indx = 0; indx < tmpArray2.Length; indx++) {
        tmpArray2[indx] = BufferHelpers.ReverseByteOrder(tmpArray2[indx]);
      }

      Assert.Equal<decimal[]>(tmpArray, tmpArray2);
    }
  }
}
