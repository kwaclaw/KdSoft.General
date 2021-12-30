using System;
using System.Text;
using KdSoft.Utils;

namespace KdSoft.Serialization
{
  /// <summary>Base class for all <see cref="ReferenceField{T, F}"/> classes
  /// that are designed to cooperate with <see cref="Formatter"/>.</summary>
  /// <typeparam name="T">Reference type that the class will serialize/deserialize.</typeparam>
  public abstract class ReferenceField<T>: ReferenceField<T, Formatter> where T : class
  {
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fmt"><see cref="Formatter"/> to register this instance with.</param>
    /// <param name="isDefault"><c>true</c> if registereing as the default field instance
    /// for the given type, <c>false</c> otherwise.</param>
    protected ReferenceField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <summary>Gives access to <see cref="Formatter">Formatter's</see>
    /// internal <c>valueIndex</c> field.</summary>
    protected int ValueIndex {
      get { return Fmt.valueIndx; }
      set { Fmt.valueIndx = value; }
    }
  }

  /// <summary>Field representing <c>byte</c> arrays.</summary>
  public class BlobField: ReferenceField<byte[]>
  {
    /// <inheritdoc />
    public BlobField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, byte[] value) {
      Fmt.Converter.WriteBytes(value.Length, target, ref Fmt.valueIndx);
      var source = new ReadOnlySpan<byte>(value);
      Fmt.Converter.WriteValueBytes(source, target, ref Fmt.valueIndx);
      //System.Buffer.BlockCopy(value, 0, target, Fmt.valueIndx, value.Length);
      //Fmt.valueIndx += value.Length;
    }

    /// <inheritdoc />
    protected override void DeserializeInstance(ReadOnlySpan<byte> source, ref byte[]? instance) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out int len);
      if (instance == null || instance.Length != len)
        instance = new byte[len];
    }

    /// <inheritdoc />
    protected override void DeserializeMembers(ReadOnlySpan<byte> source, byte[] instance) {
      var target = new Span<byte>(instance);
      Fmt.Converter.WriteValueBytes(source, target, ref Fmt.valueIndx);
      //System.Buffer.BlockCopy(source, Fmt.valueIndx, instance, 0, instance.Length);
      //Fmt.valueIndx += instance.Length;
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out int len);
      Fmt.valueIndx += len;
    }
  }

  /// <summary>Field representing fixed size <c>byte</c> arrays.</summary>
  /// <remarks>Fixed size byte buffer field (as opposed to <see cref="BlobField"/>).</remarks>
  public class BinaryField: ReferenceField<byte[]>
  {
    private int size;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fmt"><see cref="Formatter"/> to register this instance with.</param>
    /// <param name="isDefault"><c>true</c> if registereing as the default field instance
    /// for the given type, <c>false</c> otherwise.</param>
    /// <param name="size">Fixed size of <c>byte</c> array to be serialized/deserialized.</param>
    public BinaryField(Formatter fmt, bool isDefault, int size) : base(fmt, isDefault) {
      this.size = size;
    }

    /// <summary>Size (fixed) of <c>byte</c> array to be serialized/deserialized.</summary>
    public int Size {
      get { return size; }
    }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, byte[] value) {
      int len = value.Length;
      if (len > size)
        len = size;
      var source = new ReadOnlySpan<byte>(value, 0, len);
      Fmt.Converter.WriteValueBytes(source, target, ref Fmt.valueIndx);
      //System.Buffer.BlockCopy(value, 0, target, Fmt.valueIndx, len);
      //int valIndx = Fmt.valueIndx + len;
      // for missing bytes pad with zeros
      int valIndx = Fmt.valueIndx;
      for (; len < size; len++)
        target[valIndx++] = 0;
      Fmt.valueIndx = valIndx;
    }

    /// <inheritdoc />
    protected override void DeserializeInstance(ReadOnlySpan<byte> source, ref byte[]? instance) {
      if (instance == null || instance.Length != size)
        instance = new byte[size];

      var target = new Span<byte>(instance);
      Fmt.Converter.WriteValueBytes(source, target, ref Fmt.valueIndx);
      //System.Buffer.BlockCopy(target, Fmt.valueIndx, instance, 0, size);
      //Fmt.valueIndx += size;
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.valueIndx += size;
    }
  }

  /// <summary>Field representing Unicode <c>string</c> values.</summary>
  /// <remarks>Serializes string value in UTF-8 encoding, without null-terminator.
  /// If the string value itself contains separators (e.g. null-terminators) one
  /// can store multiple strings with it.</remarks>
  public class StringField: ReferenceField<String>
  {
    UTF8Encoding utf8;
    BufferPool buffers;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fmt"><see cref="Formatter"/> to register this instance with.</param>
    /// <param name="isDefault"><c>true</c> if registereing as the default field instance
    /// for the given type, <c>false</c> otherwise.</param>
    /// <param name="buffers">Buffer pool to use for encoding and decoding.</param>
    public StringField(Formatter fmt, bool isDefault, BufferPool buffers) : base(fmt, isDefault) {
      utf8 = new UTF8Encoding();
      this.buffers = buffers;
    }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, string value) {
      //var bytes = buffers.Acquire(utf8.GetByteCount(value));
      // buffers.Acquire may return a larger size than we requested!
      var bytes = buffers.Acquire(value.Length * 4);
      try {
        int len = utf8.GetBytes(value, 0, value.Length, bytes, 0);
        // write length prefix
        Fmt.Converter.WriteBytes(len, target, ref Fmt.valueIndx);
        // write UTF8 encoded characters
        var utf8Span = new ReadOnlySpan<byte>(bytes, 0, len);
        Fmt.Converter.WriteValueBytes(utf8Span, target, ref Fmt.valueIndx);
      }
      finally {
        buffers.Return(bytes);
      }
    }

    /// <inheritdoc />
    protected override void DeserializeInstance(ReadOnlySpan<byte> source, ref string? instance) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out int len);

      var bytes = buffers.Acquire(len);
      try {
        // buffers.Acquire may return a larger size than we requested!
        var byteSpan = new Span<byte>(bytes, 0, len);
        Fmt.Converter.ReadValueBytes(source, ref Fmt.valueIndx, byteSpan);
        instance = utf8.GetString(bytes, 0, len);
      }
      finally {
        buffers.Return(bytes);
      }
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out int len);
      Fmt.valueIndx += len;
    }
  }

  /// <summary>Field representing <c>UInt16</c> arrays.</summary>
  [CLSCompliant(false)]
  public class UShortArrayField: ReferenceField<UInt16[]>
  {
    /// <inheritdoc />
    public UShortArrayField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, ushort[] value) {
      int len = value.Length * sizeof(ushort);
      // write length prefix
      Fmt.Converter.WriteBytes(len, target, ref Fmt.valueIndx);
      Fmt.Converter.WriteBytes(new ReadOnlySpan<ushort>(value), target, ref Fmt.valueIndx);
    }

    /// <inheritdoc />
    protected override void DeserializeInstance(ReadOnlySpan<byte> source, ref ushort[]? instance) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out int count);
      count = count / sizeof(ushort);
      instance = new ushort[count];
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, instance);
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out int len);
      Fmt.valueIndx += len;
    }
  }

  /// <summary>Field representing <c>Char</c> arrays.</summary>
  /// <remarks>Serializes each character as <c>UInt16</c>, that is, in its native
  /// .NET encoding. Can be used to serialize strings encoded as UTF-16.</remarks>
  public class CharArrayField: ReferenceField<Char[]>
  {
    /// <inheritdoc />
    public CharArrayField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, char[] value) {
      int len = value.Length * sizeof(char);
      // write length prefix
      Fmt.Converter.WriteBytes(len, target, ref Fmt.valueIndx);
      Fmt.Converter.WriteBytes(new ReadOnlySpan<char>(value), target, ref Fmt.valueIndx);
    }

    /// <inheritdoc />
    protected override void DeserializeInstance(ReadOnlySpan<byte> source, ref char[]? instance) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out int count);
      count = count / sizeof(char);
      instance = new char[count];
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, instance);
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out int len);
      Fmt.valueIndx += len;
    }
  }

  /// <summary>Field representing fixed size <c>Char</c> arrays.</summary>
  public class FixedCharArrayField: ReferenceField<Char[]>
  {
    private int size;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fmt"><see cref="Formatter"/> to register this instance with.</param>
    /// <param name="isDefault"><c>true</c> if registereing as the default field instance
    /// for the given type, <c>false</c> otherwise.</param>
    /// <param name="size">Size in characters (not bytes!)</param>
    public FixedCharArrayField(Formatter fmt, bool isDefault, int size) : base(fmt, isDefault) {
      this.size = size;
    }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, char[] value) {
      int count = value.Length;
      if (count > size)
        count = size;
      Fmt.Converter.WriteBytes(new ReadOnlySpan<char>(value), target, ref Fmt.valueIndx);
      // for missing characters pad with two zero bytes (each)
      if (count < size) {
        int valIndx = Fmt.valueIndx;
        for (; count < size; count++) {
          target[valIndx++] = 0;
          target[valIndx++] = 0;
        }
        Fmt.valueIndx = valIndx;
      }
    }

    /// <inheritdoc />
    protected override void DeserializeInstance(ReadOnlySpan<byte> source, ref char[]? instance) {
      if (instance == null || instance.Length != size)
        instance = new char[size];
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, instance);
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.valueIndx += size * sizeof(char);
    }
  }

  /// <summary>Field representing <c>Int16</c> arrays.</summary>
  public class ShortArrayField: ReferenceField<Int16[]>
  {
    /// <inheritdoc />
    public ShortArrayField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, short[] value) {
      int len = value.Length * sizeof(short);
      // write length prefix
      Fmt.Converter.WriteBytes(len, target, ref Fmt.valueIndx);
      Fmt.Converter.WriteBytes(new ReadOnlySpan<short>(value), target, ref Fmt.valueIndx);
    }

    /// <inheritdoc />
    protected override void DeserializeInstance(ReadOnlySpan<byte> source, ref short[]? instance) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out int count);
      count = count / sizeof(short);
      instance = new short[count];
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, instance);
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out int len);
      Fmt.valueIndx += len;
    }
  }

  /// <summary>Field representing a <c>UInt32</c> arrays.</summary>
  [CLSCompliant(false)]
  public class UIntArrayField: ReferenceField<UInt32[]>
  {
    /// <inheritdoc />
    public UIntArrayField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, uint[] value) {
      int len = value.Length * sizeof(uint);
      // write length prefix
      Fmt.Converter.WriteBytes(len, target, ref Fmt.valueIndx);
      Fmt.Converter.WriteBytes(new ReadOnlySpan<uint>(value), target, ref Fmt.valueIndx);
    }

    /// <inheritdoc />
    protected override void DeserializeInstance(ReadOnlySpan<byte> source, ref uint[]? instance) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out int count);
      count = count / sizeof(uint);
      instance = new uint[count];
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, instance);
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out int len);
      Fmt.valueIndx += len;
    }
  }

  /// <summary>Field representing a <c>Int32</c> arrays.</summary>
  public class IntArrayField: ReferenceField<Int32[]>
  {
    /// <inheritdoc />
    public IntArrayField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, int[] value) {
      int len = value.Length * sizeof(int);
      // write length prefix
      Fmt.Converter.WriteBytes(len, target, ref Fmt.valueIndx);
      Fmt.Converter.WriteBytes(new ReadOnlySpan<int>(value), target, ref Fmt.valueIndx);
    }

    /// <inheritdoc />
    protected override void DeserializeInstance(ReadOnlySpan<byte> source, ref int[]? instance) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out int count);
      count = count / sizeof(int);
      instance = new int[count];
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, instance);
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out int len);
      Fmt.valueIndx += len;
    }
  }
}
