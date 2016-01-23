using System;
using System.Collections.Generic;

namespace KdSoft.Utils
{
  public static class Common
  {
    public static char[] HexChars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

    public static string BytesToHexStr(byte[] data) {
      int i = 0, p = 0, l = data.Length;
      char[] c = new char[l * 2];
      while (i < l) {
        byte d = data[i++];
        c[p++] = HexChars[d / 0x10];
        c[p++] = HexChars[d % 0x10];
      }
      return new string(c, 0, c.Length);
    }

    static void HexCharError(char e) {
      throw new ArgumentException("Not a hexadecimal character: '" + e + "'.");
    }

    public static byte HexCharToByte(char c) {
      byte result;
      if (c >= '0' && c <= '9')
        result = (byte)(c - '0');
      else if (c >= 'a' && c <= 'f')
        result = (byte)((c - 'a') + 10);
      else if (c >= 'A' && c <= 'F')
        result = (byte)((c - 'A') + 10);
      else {
        HexCharError(c);
        result = 0;
      }
      return result;
    }

    public static byte[] HexStrToBytes(string hexStr) {
      if (hexStr.Length % 2 != 0)
        throw new ArgumentException("Hexadecimal string must have an even number of characters.", "hexStr");
      byte[] result = new byte[hexStr.Length / 2];
      for (int indx = 0; indx < result.Length; indx++) {
        var strIndx = indx << 1;
        result[indx] = (byte)(HexCharToByte(hexStr[strIndx]) * 16 + HexCharToByte(hexStr[strIndx + 1]));
      }
      return result;
    }

    public static bool Equals<T>(this T[] array, T[] other) {
      if (object.ReferenceEquals(array, other))
        return true;
      if (array == null || other == null)
        return false;
      if (array.Length != other.Length)
        return false;
      for (int indx = 0; indx < array.Length; indx++)
        if (!object.Equals(array[indx], other[indx]))
          return false;
      return true;
    }

    public static bool Equals<T>(this T[] array, T[] other, IEqualityComparer<T> comparer) {
      if (object.ReferenceEquals(array, other))
        return true;
      if (array == null || other == null)
        return false;
      if (array.Length != other.Length)
        return false;
      if (comparer == null)
        comparer = EqualityComparer<T>.Default;
      for (int indx = 0; indx < array.Length; indx++)
        if (!comparer.Equals(array[indx], other[indx]))
          return false;
      return true;
    }

  }
}
