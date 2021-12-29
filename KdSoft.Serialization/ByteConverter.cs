/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2006, 2008
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;

namespace KdSoft.Serialization
{
  /// <summary>
  /// Indicates the order of bytes in a value type.
  /// </summary>
  public enum ByteOrder
  {
    /// <summary>Low order byte comes first.</summary>
    BigEndian,
    /// <summary>High order byte comes first.</summary>
    LittleEndian
  }

  /// <summary>Serializes and deserializes numeric types in a specified byte order.</summary>
  public class ByteConverter
  {
    /// <summary>Returns the byte order of this system architecture.</summary>
    public static readonly ByteOrder SystemByteOrder;

    static ByteConverter() {
      SystemByteOrder = BitConverter.IsLittleEndian ? ByteOrder.LittleEndian : ByteOrder.BigEndian;
    }


    bool reverse;
    /// <summary>
    /// Indicates if the <see cref="ByteConverter"/> reads/writes bytes in reverse system byte order.
    /// </summary>
    public bool Reverse => reverse;

    /// <summary>
    /// Constructor that specifies a specific byte order to use for reading and writing.
    /// </summary>
    public ByteConverter(ByteOrder byteOrder) {
      reverse = byteOrder != SystemByteOrder;
    }

    /// <summary>
    /// Constructor that specifies if reading/writing should be performed in system byte order or in reverse.
    /// </summary>
    public ByteConverter(bool reverse = false) {
      this.reverse = reverse;
    }

    #region WriteBytes

    /// <summary>
    /// Writes value type to byte span like <see cref="SpanHelpers.TryWriteBytes{T}(in T, Span{byte}, ref int)" />,
    /// but throws exception if not successfull.
    /// </summary>
    public void WriteValueBytes<T>(in T value, Span<byte> bytes, ref int index) where T : struct {
      if (!SpanHelpers.TryWriteBytes(in value, bytes, ref index))
        throw new SerializationException("Buffer exhausted.");
    }

    /// <summary>
    /// Writes value type to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(in T, Span{byte}, ref int)"/>
    [CLSCompliant(false)]
    public void WriteBytes(UInt16 value, Span<byte> bytes, ref int index) {
      if (reverse)
        value = value.ReverseByteOrder();
      WriteValueBytes(value, bytes, ref index);
    }

    /// <summary>
    /// Writes value type to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(in T, Span{byte}, ref int)"/>
    public void WriteBytes(Int16 value, Span<byte> bytes, ref int index) {
      if (reverse)
        value = value.ReverseByteOrder();
      WriteValueBytes(value, bytes, ref index);
    }

    /// <summary>
    /// Writes value type to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(in T, Span{byte}, ref int)"/>
    public void WriteBytes(Char value, Span<byte> bytes, ref int index) {
      if (reverse)
        value = value.ReverseByteOrder();
      WriteValueBytes(value, bytes, ref index);
    }

    /// <summary>
    /// Writes value type to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(in T, Span{byte}, ref int)"/>
    [CLSCompliant(false)]
    public void WriteBytes(UInt32 value, Span<byte> bytes, ref int index) {
      if (reverse)
        value = value.ReverseByteOrder();
      WriteValueBytes(value, bytes, ref index);
    }

    /// <summary>
    /// Writes value type to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(in T, Span{byte}, ref int)"/>
    public void WriteBytes(Int32 value, Span<byte> bytes, ref int index) {
      if (reverse)
        value = value.ReverseByteOrder();
      WriteValueBytes(value, bytes, ref index);
    }

    /// <summary>
    /// Writes value type to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(in T, Span{byte}, ref int)"/>
    public void WriteBytes(Single value, Span<byte> bytes, ref int index) {
      if (reverse)
        value = value.ReverseByteOrder();
      WriteValueBytes(value, bytes, ref index);
    }

    /// <summary>
    /// Writes value type to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(in T, Span{byte}, ref int)"/>
    [CLSCompliant(false)]
    public void WriteBytes(UInt64 value, Span<byte> bytes, ref int index) {
      if (reverse)
        value = value.ReverseByteOrder();
      WriteValueBytes(value, bytes, ref index);
    }

    /// <summary>
    /// Writes value type to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(in T, Span{byte}, ref int)"/>
    public void WriteBytes(Int64 value, Span<byte> bytes, ref int index) {
      if (reverse)
        value = value.ReverseByteOrder();
      WriteValueBytes(value, bytes, ref index);
    }

    /// <summary>
    /// Writes value type to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(in T, Span{byte}, ref int)"/>
    public void WriteBytes(Double value, Span<byte> bytes, ref int index) {
      if (reverse)
        value = value.ReverseByteOrder();
      WriteValueBytes(value, bytes, ref index);
    }

    /// <summary>
    /// Writes value type to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(in T, Span{byte}, ref int)"/>
    public void WriteBytes(Decimal value, Span<byte> bytes, ref int index) {
      if (reverse)
        value = value.ReverseByteOrder();
      WriteValueBytes(value, bytes, ref index);
    }

    #endregion

    #region Span WriteBytes

    /// <summary>
    /// Writes value type span to byte span like <see cref="SpanHelpers.TryWriteBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int)"/>,
    /// but throws exception if not successfull.
    /// </summary>
    public void WriteValueBytes<T>(ReadOnlySpan<T> values, Span<byte> bytes, ref int index) where T : struct {
      if (!SpanHelpers.TryWriteBytes<T>(values, bytes, ref index))
        throw new SerializationException("Buffer exhausted.");
    }

    /// <summary>
    /// Writes value type span to byte span like <see cref="SpanHelpers.TryWriteBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int, Func{T, T})"/>,
    /// but throws exception if not successfull.
    /// </summary>
    public void WriteValueBytes<T>(ReadOnlySpan<T> values, Span<byte> bytes, ref int index, Func<T, T> beforeWrite) where T : struct {
      if (!SpanHelpers.TryWriteBytes<T>(values, bytes, ref index, beforeWrite))
        throw new SerializationException("Buffer exhausted.");
    }

    /// <summary>
    /// Writes value type span to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int)"/>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int, Func{T, T})"/>
    [CLSCompliant(false)]
    public void WriteBytes(ReadOnlySpan<UInt16> values, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteValueBytes(values, bytes, ref index, ValueTypeExtensions.ReverseByteOrder);
      else
        WriteValueBytes(values, bytes, ref index);
    }

    /// <summary>
    /// Writes value type span to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int)"/>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int, Func{T, T})"/>
    public void WriteBytes(ReadOnlySpan<Int16> values, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteValueBytes(values, bytes, ref index, ValueTypeExtensions.ReverseByteOrder);
      else
        WriteValueBytes(values, bytes, ref index);
    }

    /// <summary>
    /// Writes value type span to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int)"/>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int, Func{T, T})"/>
    public void WriteBytes(ReadOnlySpan<Char> values, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteValueBytes(values, bytes, ref index, ValueTypeExtensions.ReverseByteOrder);
      else
        WriteValueBytes(values, bytes, ref index);
    }

    /// <summary>
    /// Writes value type span to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int)"/>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int, Func{T, T})"/>
    [CLSCompliant(false)]
    public void WriteBytes(ReadOnlySpan<UInt32> values, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteValueBytes(values, bytes, ref index, ValueTypeExtensions.ReverseByteOrder);
      else
        WriteValueBytes(values, bytes, ref index);
    }

    /// <summary>
    /// Writes value type span to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int)"/>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int, Func{T, T})"/>
    public void WriteBytes(ReadOnlySpan<Int32> value, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteValueBytes(value, bytes, ref index, ValueTypeExtensions.ReverseByteOrder);
      else
        WriteValueBytes(value, bytes, ref index);
    }

    /// <summary>
    /// Writes value type span to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int)"/>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int, Func{T, T})"/>
    public void WriteBytes(ReadOnlySpan<Single> values, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteValueBytes(values, bytes, ref index, ValueTypeExtensions.ReverseByteOrder);
      else
        WriteValueBytes(values, bytes, ref index);
    }

    /// <summary>
    /// Writes value type span to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int)"/>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int, Func{T, T})"/>
    [CLSCompliant(false)]
    public void WriteBytes(ReadOnlySpan<UInt64> values, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteValueBytes(values, bytes, ref index, ValueTypeExtensions.ReverseByteOrder);
      else
        WriteValueBytes(values, bytes, ref index);
    }

    /// <summary>
    /// Writes value type span to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int)"/>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int, Func{T, T})"/>
    public void WriteBytes(ReadOnlySpan<Int64> values, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteValueBytes(values, bytes, ref index, ValueTypeExtensions.ReverseByteOrder);
      else
        WriteValueBytes(values, bytes, ref index);
    }

    /// <summary>
    /// Writes value type span to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int)"/>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int, Func{T, T})"/>
    public void WriteBytes(ReadOnlySpan<Double> values, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteValueBytes(values, bytes, ref index, ValueTypeExtensions.ReverseByteOrder);
      else
        WriteValueBytes(values, bytes, ref index);
    }

    /// <summary>
    /// Writes value type span to byte span in configured byte order.
    /// </summary>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int)"/>
    /// <seealso cref="WriteValueBytes{T}(ReadOnlySpan{T}, Span{byte}, ref int, Func{T, T})"/>
    public void WriteBytes(ReadOnlySpan<Decimal> values, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteValueBytes(values, bytes, ref index, ValueTypeExtensions.ReverseByteOrder);
      else
        WriteValueBytes(values, bytes, ref index);
    }

    #endregion

    #region ReadBytes

    /// <summary>
    /// Reads value type from byte span like <see cref="SpanHelpers.TryReadBytes{T}(ReadOnlySpan{byte}, ref int, ref T)" />,
    /// but throws exception if not successfull.
    /// </summary>
    public void ReadValueBytes<T>(ReadOnlySpan<byte> bytes, ref int index, out T value) where T : struct {
      value = default;
      if (!SpanHelpers.TryReadBytes(bytes, ref index, ref value))
        throw new SerializationException("Buffer exhausted.");
    }

    /// <summary>
    /// Reads value type from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, out T)"/>
    [CLSCompliant(false)]
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out UInt16 value) {
      ReadValueBytes(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    /// <summary>
    /// Reads value type from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, out T)"/>
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out Int16 value) {
      ReadValueBytes(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    /// <summary>
    /// Reads value type from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, out T)"/>
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out Char value) {
      ReadValueBytes(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    /// <summary>
    /// Reads value type from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, out T)"/>
    [CLSCompliant(false)]
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out UInt32 value) {
      ReadValueBytes(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    /// <summary>
    /// Reads value type from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, out T)"/>
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out Int32 value) {
      ReadValueBytes(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    /// <summary>
    /// Reads value type from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, out T)"/>
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out Single value) {
      ReadValueBytes(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    /// <summary>
    /// Reads value type from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, out T)"/>
    [CLSCompliant(false)]
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out UInt64 value) {
      ReadValueBytes(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    /// <summary>
    /// Reads value type from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, out T)"/>
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out Int64 value) {
      ReadValueBytes(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    /// <summary>
    /// Reads value type from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, out T)"/>
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out Double value) {
      ReadValueBytes(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    /// <summary>
    /// Reads value type from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, out T)"/>
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out Decimal value) {
      ReadValueBytes(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    #endregion

    #region Span ReadBytes

    /// <summary>
    /// Reads value type span from byte span like <see cref="SpanHelpers.TryReadBytes{T}(ReadOnlySpan{byte}, ref int, Span{T})" />,
    /// but throws exception if not successfull.
    /// </summary>
    public void ReadValueBytes<T>(ReadOnlySpan<byte> bytes, ref int index, Span<T> values) where T : struct {
      if (!SpanHelpers.TryReadBytes(bytes, ref index, values))
        throw new SerializationException("Buffer exhausted.");
    }

    /// <summary>
    /// Reads value type span from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, Span{T})"/>
    [CLSCompliant(false)]
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, Span<UInt16> values) {
      ReadValueBytes(bytes, ref index, values);
      if (reverse)
        SpanHelpers.ReverseByteOrder(values);
    }

    /// <summary>
    /// Reads value type span from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, Span{T})"/>
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, Span<Int16> values) {
      ReadValueBytes(bytes, ref index, values);
      if (reverse)
        SpanHelpers.ReverseByteOrder(values);
    }

    /// <summary>
    /// Reads value type span from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, Span{T})"/>
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, Span<Char> values) {
      ReadValueBytes(bytes, ref index, values);
      if (reverse)
        SpanHelpers.ReverseByteOrder(values);
    }

    /// <summary>
    /// Reads value type span from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, Span{T})"/>
    [CLSCompliant(false)]
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, Span<UInt32> values) {
      ReadValueBytes(bytes, ref index, values);
      if (reverse)
        SpanHelpers.ReverseByteOrder(values);
    }

    /// <summary>
    /// Reads value type span from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, Span{T})"/>
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, Span<Int32> values) {
      ReadValueBytes(bytes, ref index, values);
      if (reverse)
        SpanHelpers.ReverseByteOrder(values);
    }

    /// <summary>
    /// Reads value type span from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, Span{T})"/>
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, Span<Single> values) {
      ReadValueBytes(bytes, ref index, values);
      if (reverse)
        SpanHelpers.ReverseByteOrder(values);
    }

    /// <summary>
    /// Reads value type span from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, Span{T})"/>
    [CLSCompliant(false)]
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, Span<UInt64> values) {
      ReadValueBytes(bytes, ref index, values);
      if (reverse)
        SpanHelpers.ReverseByteOrder(values);
    }

    /// <summary>
    /// Reads value type span from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, Span{T})"/>
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, Span<Int64> values) {
      ReadValueBytes(bytes, ref index, values);
      if (reverse)
        SpanHelpers.ReverseByteOrder(values);
    }

    /// <summary>
    /// Reads value type span from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, Span{T})"/>
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, Span<Double> values) {
      ReadValueBytes(bytes, ref index, values);
      if (reverse)
        SpanHelpers.ReverseByteOrder(values);
    }

    /// <summary>
    /// Reads value type span from byte span with configured byte order.
    /// </summary>
    /// <seealso cref="ReadValueBytes{T}(ReadOnlySpan{byte}, ref int, Span{T})"/>
    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, Span<Decimal> values) {
      ReadValueBytes(bytes, ref index, values);
      if (reverse)
        SpanHelpers.ReverseByteOrder(values);
    }

    #endregion
  }
}
