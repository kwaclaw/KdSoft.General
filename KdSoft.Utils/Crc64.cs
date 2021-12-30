using System;
using System.Security.Cryptography;

namespace KdSoft.Utils
{
  public class Crc64: HashAlgorithm
  {
    ulong[] CrcTable;
    ulong initCrc;
    ulong crc;

    public const string Name = "KdSoft.Utils.Crc64";

    static Crc64() {
#if !NETSTANDARD1_3
      CryptoConfig.AddAlgorithm(typeof(Crc64), Name, "Crc64");
#endif
    }

    [CLSCompliant(false)]
    public Crc64(ulong polynome, ulong initCrc) {
      GC.SuppressFinalize(this);
#if !NETSTANDARD1_3
      this.HashSizeValue = 8;
#endif
      this.initCrc = initCrc;
      this.crc = initCrc;  // initialize CRC value
      CrcTable = MakeCrcTable(polynome);
    }

    public Crc64(long polynome, long initCrc) : this(unchecked((ulong)polynome), unchecked((ulong)polynome)) { }

    public Crc64() : this(0x95AC9329AC4BC9B5, 0xFFFFFFFFFFFFFFFF) { }

    ulong[] MakeCrcTable(ulong polynome) {
      ulong[] table = new ulong[256];
      uint i, j;
      ulong part;
      for (i = 0; i < 256; i++) {
        part = i;
        for (j = 0; j < 8; j++) {
          if ((part & 1) != 0)
            part = (part >> 1) ^ polynome;
          else
            part >>= 1;
        }
        table[i] = part;
      }
      return table;
    }

    protected override void HashCore(byte[] array, int ibStart, int cbSize) {
      int ibEnd = ibStart + cbSize;
      ulong tmpCrc = crc;
      for (int indx = ibStart; indx < ibEnd; indx++)
        tmpCrc = CrcTable[(tmpCrc ^ array[indx]) & 0xff] ^ (tmpCrc >> 8);
      crc = tmpCrc;
    }

    protected override byte[] HashFinal() {
      byte[] hash = new byte[8];
      ulong tmpCrc = crc;
      for (int indx = 0; indx < 8; indx++) {
        hash[indx] = (byte)(tmpCrc & 0xFF);
        tmpCrc >>= 8;
      }
      return hash;
    }

    public override void Initialize() {
      crc = initCrc;
    }

    public long InitialCrc {
      get { return unchecked((long)initCrc); }
    }

    [CLSCompliant(false)]
    public ulong Crc {
      get { return crc; }
    }
  }
}
