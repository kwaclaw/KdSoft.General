using System;

namespace KdSoft.Serialization
{
  /// <summary>Base class for all <see cref="ValueField{T, F}"/> classes
  /// that are designed to cooperate with <see cref="Formatter"/>.</summary>
  /// <typeparam name="T">Value type that the class will serialize/deserialize.</typeparam>
  public abstract class ValueField<T>: ValueField<T, Formatter> where T : struct
  {
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fmt"><see cref="Formatter"/> to register this instance with.</param>
    /// <param name="isDefault"><c>true</c> if registereing as the default field instance
    /// for the given type, <c>false</c> otherwise.</param>
    protected ValueField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <summary>Gives access to <see cref="Formatter">Formatter's</see>
    /// internal <c>valueIndex</c> field.</summary>
    protected int ValueIndex {
      get { return Fmt.valueIndx; }
      set { Fmt.valueIndx = value; }
    }
  }

  /// <summary>Field representing <c>Byte</c> values.</summary>
  public class ByteField: ValueField<Byte>
  {
    /// <inheritdoc />
    public ByteField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, ref byte value) {
      target[Fmt.valueIndx++] = value;
    }

    /// <inheritdoc />
    protected override void DeserializeValue(ReadOnlySpan<byte> source, ref byte value) {
      value = source[Fmt.valueIndx++];
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.valueIndx++;
    }
  }

  /// <summary>Field representing <c>Boolean</c> values.</summary>
  /// <remarks>Stored as <c>Byte</c> values <c>0xFF, 0x00</c>.</remarks>
  public class BoolField: ValueField<Boolean>
  {
    /// <inheritdoc />
    public BoolField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, ref bool value) {
      target[Fmt.valueIndx++] = value ? (byte)0xFF : (byte)0;
    }

    /// <inheritdoc />
    protected override void DeserializeValue(ReadOnlySpan<byte> source, ref bool value) {
      value = source[Fmt.valueIndx++] == 0 ? false : true;
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.valueIndx++;
    }
  }

  /// <summary>Field representing <c>SByte</c> values.</summary>
  /// <remarks>We serialize signed integers with their sign-bit (high-order bit)
  /// inverted, so that with big-endian byte/bit order negative numbers are
  /// sorted first.</remarks>
  [CLSCompliant(false)]
  public class SByteField: ValueField<SByte>
  {
    /// <inheritdoc />
    public SByteField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, ref sbyte value) {
      target[Fmt.valueIndx++] = unchecked((byte)(-value));
    }

    /// <inheritdoc />
    protected override void DeserializeValue(ReadOnlySpan<byte> source, ref sbyte value) {
      value = unchecked((sbyte)-source[Fmt.valueIndx++]);
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.valueIndx++;
    }
  }

  /// <summary>Field representing <c>UInt16</c> values.</summary>
  [CLSCompliant(false)]
  public class UShortField: ValueField<UInt16>
  {
    /// <inheritdoc />
    public UShortField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, ref ushort value) {
      Fmt.Converter.WriteBytes(value, target, ref Fmt.valueIndx);
    }

    /// <inheritdoc />
    protected override void DeserializeValue(ReadOnlySpan<byte> source, ref ushort value) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out value);
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.valueIndx += sizeof(ushort);
    }
  }

  /// <summary>Field representing <c>Int16</c> values.</summary>
  /// <remarks>We serialize signed integers with their sign-bit (high-order bit)
  /// inverted, so that with big-endian byte order negative numbers are sorted first
  /// when comparing them in lexical order (as unsigned byte arrays).</remarks>
  public class ShortField: ValueField<Int16>
  {
    /// <inheritdoc />
    public ShortField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, ref short value) {
      Fmt.Converter.WriteBytes(unchecked((short)-value), target, ref Fmt.valueIndx);
    }

    /// <inheritdoc />
    protected override void DeserializeValue(ReadOnlySpan<byte> source, ref short value) {
      short tmpValue;
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out tmpValue);
      value = unchecked((short)-tmpValue);
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.valueIndx += sizeof(short);
    }
  }

  /// <summary>Field representing <c>Char</c> values.</summary>
  public class CharField: ValueField<Char>
  {
    /// <inheritdoc />
    public CharField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, ref char value) {
      Fmt.Converter.WriteBytes(value, target, ref Fmt.valueIndx);
    }

    /// <inheritdoc />
    protected override void DeserializeValue(ReadOnlySpan<byte> source, ref char value) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out value);
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.valueIndx += sizeof(ushort);
    }
  }

  /// <summary>Field representing <c>UInt32</c> values.</summary>
  [CLSCompliant(false)]
  public class UIntField: ValueField<UInt32>
  {
    /// <inheritdoc />
    public UIntField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, ref uint value) {
      Fmt.Converter.WriteBytes(value, target, ref Fmt.valueIndx);
    }

    /// <inheritdoc />
    protected override void DeserializeValue(ReadOnlySpan<byte> source, ref uint value) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out value);
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.valueIndx += sizeof(uint);
    }
  }

  /// <summary>Field representing <c>Int32</c> values.</summary>
  /// <remarks>We serialize signed integers with their sign-bit (high-order bit)
  /// inverted, so that with big-endian byte order negative numbers are sorted first
  /// when comparing them in lexical order (as unsigned byte arrays).</remarks>
  public class IntField: ValueField<Int32>
  {
    /// <inheritdoc />
    public IntField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, ref int value) {
      Fmt.Converter.WriteBytes(-value, target, ref Fmt.valueIndx);
    }

    /// <inheritdoc />
    protected override void DeserializeValue(ReadOnlySpan<byte> source, ref int value) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out value);
      value = -value;
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.valueIndx += sizeof(int);
    }
  }

  /// <summary>Field representing <c>UInt64</c> values.</summary>
  [CLSCompliant(false)]
  public class ULongField: ValueField<UInt64>
  {
    /// <inheritdoc />
    public ULongField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, ref ulong value) {
      Fmt.Converter.WriteBytes(value, target, ref Fmt.valueIndx);
    }

    /// <inheritdoc />
    protected override void DeserializeValue(ReadOnlySpan<byte> source, ref ulong value) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out value);
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.valueIndx += sizeof(ulong);
    }
  }

  /// <summary>Field representing <c>Int64</c> values.</summary>
  /// <remarks>We serialize signed integers with their sign-bit (high-order bit)
  /// inverted, so that with big-endian byte order negative numbers are sorted first
  /// when comparing them in lexical order (as unsigned byte arrays).</remarks>
  public class LongField: ValueField<Int64>
  {
    /// <inheritdoc />
    public LongField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, ref long value) {
      Fmt.Converter.WriteBytes(-value, target, ref Fmt.valueIndx);
    }

    /// <inheritdoc />
    protected override void DeserializeValue(ReadOnlySpan<byte> source, ref long value) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out value);
      value = -value;
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.valueIndx += sizeof(long);
    }
  }

  /// <summary>Field representing <c>Decimal</c> values.</summary>
  /// <remarks>No sign bit reversion here, as the first part contains the scaling
  /// exponent, which grows larger the smaller the number is. It is recommended
  /// to provide a comparison function.</remarks>
  public class DecimalField: ValueField<Decimal>
  {
    /// <inheritdoc />
    public DecimalField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, ref decimal value) {
      Fmt.Converter.WriteBytes(value, target, ref Fmt.valueIndx);
    }

    /// <inheritdoc />
    protected override void DeserializeValue(ReadOnlySpan<byte> source, ref decimal value) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out value);
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.valueIndx += 4 * sizeof(UInt32);  // serialized as four 32bit values
    }
  }

  /// <summary>Field representing <c>Single</c> values.</summary>
  /// <remarks>Stored in standard IEEE 754 bit representation.</remarks>
  public class SingleField: ValueField<Single>
  {
    /// <inheritdoc />
    public SingleField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, ref float value) {
      Fmt.Converter.WriteBytes(value, target, ref Fmt.valueIndx);
    }

    /// <inheritdoc />
    protected override void DeserializeValue(ReadOnlySpan<byte> source, ref float value) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out value);
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.valueIndx += sizeof(UInt32);  // serialized as 32bit value
    }
  }

  /// <summary>Field representing <c>Double</c> values.</summary>
  /// <remarks>Stored in standard IEEE 754 bit representation.</remarks>
  public class DoubleField: ValueField<Double>
  {
    /// <inheritdoc />
    public DoubleField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, ref double value) {
      Fmt.Converter.WriteBytes(value, target, ref Fmt.valueIndx);
    }

    /// <inheritdoc />
    protected override void DeserializeValue(ReadOnlySpan<byte> source, ref double value) {
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out value);
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.valueIndx += sizeof(UInt64);  // serialized as 64bit value
    }
  }

  /// <summary>Field representing UTC <c>DateTime</c> values.</summary>
  /// <remarks>Serialized as 64bit integer. Converts to UTC time when Value property
  /// is assigned. Time values are measured in 100-nanosecond units called ticks,
  /// and a particular date is the number of ticks since 12:00 midnight, January 1,
  /// 0001 A.D. (C.E.) in the Gregorian calendar.</remarks>
  public class DateTimeField: ValueField<DateTime>
  {
    /// <inheritdoc />
    public DateTimeField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, ref DateTime value) {
      Fmt.Converter.WriteBytes(value.Ticks, target, ref Fmt.valueIndx);
    }

    /// <inheritdoc />
    protected override void DeserializeValue(ReadOnlySpan<byte> source, ref DateTime value) {
      long ticks = default;
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out ticks);
      value = new DateTime(ticks, DateTimeKind.Utc);
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.valueIndx += sizeof(long);
    }
  }

  /// <summary>Field representing <c>TimeSpan</c> values.</summary>
  /// <remarks>Serialized as 64bit integer.</remarks>
  public class TimeSpanField: ValueField<TimeSpan>
  {
    /// <inheritdoc />
    public TimeSpanField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, ref TimeSpan value) {
      Fmt.Converter.WriteBytes(value.Ticks, target, ref Fmt.valueIndx);
    }

    /// <inheritdoc />
    protected override void DeserializeValue(ReadOnlySpan<byte> source, ref TimeSpan value) {
      long ticks = default;
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out ticks);
      value = new TimeSpan(ticks);
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.valueIndx += sizeof(long);
    }
  }

  /// <summary>Field representing <c>DateTimeOffset</c> values.</summary>
  /// <remarks>Serialized as 64bit integer (UTC ticks), followed by 16 bit integer (offset).</remarks>
  public class DateTimeOffsetField: ValueField<DateTimeOffset>
  {
    /// <inheritdoc />
    public DateTimeOffsetField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

    /// <inheritdoc />
    protected override void SerializeValue(Span<byte> target, ref DateTimeOffset value) {
      Fmt.Converter.WriteBytes(value.Ticks, target, ref Fmt.valueIndx);
      short offsetMinutes = unchecked((short)value.Offset.TotalMinutes);
      Fmt.Converter.WriteBytes(offsetMinutes, target, ref Fmt.valueIndx);
    }

    /// <inheritdoc />
    protected override void DeserializeValue(ReadOnlySpan<byte> source, ref DateTimeOffset value) {
      long ticks = default;
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out ticks);
      short offsetMinutes = default;
      Fmt.Converter.ReadBytes(source, ref Fmt.valueIndx, out offsetMinutes);
      value = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offsetMinutes));
    }

    /// <inheritdoc />
    protected override void SkipValue(ReadOnlySpan<byte> source) {
      Fmt.valueIndx += (sizeof(long) + sizeof(short));
    }
  }
}
