using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace KdSoft.Utils
{
  public static class CryptUtils
  {
    // the hash salt is inserted in the middle of the value
    public static byte[] HashString(string value, string salt, HashAlgorithm hasher) {
      int partLength = value.Length / 2;
      // make sure we allocate enough buffer - a UTF-16 code point has 2 bytes
      byte[] inputBytes = new byte[(value.Length + salt.Length) * 2];
      int written = Encoding.Unicode.GetBytes(value, 0, partLength, inputBytes, 0);
      written += Encoding.Unicode.GetBytes(salt, 0, salt.Length, inputBytes, written);
      written += Encoding.Unicode.GetBytes(value, partLength, value.Length - partLength, inputBytes, written);
      return hasher.ComputeHash(inputBytes);
    }

  }
}
