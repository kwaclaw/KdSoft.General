﻿/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2006, 2008
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KdSoft.Serialization.Buffer
{
  public enum ByteOrder
  {
    BigEndian,
    LittleEndian
  }

  /// <summary>Union used for re-interpreting 16 bit numeric types as byte arrays and vice-versa.</summary>
  [StructLayout(LayoutKind.Explicit)]
  public struct ShortBytes
  {
    /// <summary><c>Int16</c> version of the value.</summary>
    [FieldOffset(0)]
    public readonly Int16 ShortValue;

    /// <summary><c>UInt16</c> version of the value.</summary>
    [FieldOffset(0), CLSCompliant(false)]
    public readonly UInt16 UShortValue;

    /// <summary><c>Char</c> version of the value.</summary>
    [FieldOffset(0)]
    public readonly Char CharValue;

    [FieldOffset(0)]
    Byte byte0;

    /// <summary><c>Byte 0</c> of the value.</summary>
    public Byte Byte0 { get { return byte0; } }

    [FieldOffset(1)]
    Byte byte1;

    /// <summary><c>Byte 1</c> of the value.</summary>
    public Byte Byte1 { get { return byte1; } }

    /// <summary>Constructor taking an <c>Int16</c> value.</summary>
    public ShortBytes(Int16 value) {
      this = new ShortBytes();
      ShortValue = value;
    }

    /// <summary>Constructor taking an <c>UInt16</c> value.</summary>
    [CLSCompliant(false)]
    public ShortBytes(UInt16 value) {
      this = new ShortBytes();
      UShortValue = value;
    }

    /// <summary>Constructor taking a <c>Char</c> value.</summary>
    public ShortBytes(Char value) {
      this = new ShortBytes();
      CharValue = value;
    }

    /// <summary>Returns copy with reversed byte order.</summary>
    public ShortBytes ReverseByteOrder() {
      var result = new ShortBytes();
      result.byte0 = byte1;
      result.byte1 = byte0;
      return result;
    }

    /// <summary>Returns new instance initialized from <c>byte array</c>.</summary>
    /// <param name="value">Byte array to initialize the instance with.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public static ShortBytes FromBytes(ReadOnlySpan<byte> value, ref int index) {
      ShortBytes result = new ShortBytes();
      result.byte0 = value[index++];
      result.byte1 = value[index++];
      return result;
    }

    /// <summary>Returns new instance initialized from reversed <c>byte array</c>.</summary>
    /// <param name="value">Byte array to initialize the instance with.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public static ShortBytes FromBytesReversed(ReadOnlySpan<byte> value, ref int index) {
      ShortBytes result = new ShortBytes();
      result.byte1 = value[index++];
      result.byte0 = value[index++];
      return result;
    }

    //TODO For ToBytes (but not the reverse version) use BitConverter.TryWriteBytes when it becomes available?

    // Note: we may not need a ref int index argument, because we can pass a Span<byte> created as a Slice() from the overall Span<byte>

    /// <summary>Writes value to byte <see cref="Span{T}"/>.</summary>
    /// <param name="bytes">Byte span to write value to.</param>
    /// <returns><c>true</c> if Span was large enough, <c>false</c>otherwise.</returns>
    public bool TryWriteBytes(Span<byte> bytes) {
      //return BitConverter.TryWriteBytes(bytes, UShortValue);
      if (bytes.Length < sizeof(ushort))
        return false;
      Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(bytes), UShortValue);
      return true;
    }

    public bool WriteBytes(Span<byte> bytes, ref int index) {
      //return BitConverter.TryWriteBytes(bytes, UShortValue);
      if ((bytes.Length - index) < sizeof(ushort))
        return false;
      ref var target = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(bytes), (IntPtr)index);
      Unsafe.WriteUnaligned(ref target, UShortValue);
      index += sizeof(ushort);
      return true;
    }

    /// <summary>Writes the value to a byte array.</summary>
    /// <param name="bytes">Target byte array.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public void ToBytes(Span<byte> bytes, ref int index) {
      bytes[index++] = byte0;
      bytes[index++] = byte1;
    }

    /// <summary>Writes the value to a byte array in reversed byte order.</summary>
    /// <param name="bytes">Target byte array.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public void ToBytesReversed(Span<byte> bytes, ref int index) {
      bytes[index++] = byte1;
      bytes[index++] = byte0;
    }

    /// <summary>
    /// Defines an implicit conversion of <see cref="Int16"/> to <see cref="ShortBytes"/>.
    /// </summary>
    public static implicit operator ShortBytes(Int16 value) => new ShortBytes(value);

    /// <summary>
    /// Defines an implicit conversion of <see cref="UInt16"/> to <see cref="ShortBytes"/>.
    /// </summary>
    [CLSCompliant(false)]
    public static implicit operator ShortBytes(UInt16 value) => new ShortBytes(value);

    /// <summary>
    /// Defines an implicit conversion of <see cref="Char"/> to <see cref="ShortBytes"/>.
    /// </summary>
    [CLSCompliant(false)]
    public static implicit operator ShortBytes(Char value) => new ShortBytes(value);

    /// <summary>
    /// Defines an implicit conversion of <see cref="ShortBytes"/> to <see cref="Int16"/>.
    /// </summary>
    public static implicit operator Int16(ShortBytes value) => value.ShortValue;

    /// <summary>
    /// Defines an implicit conversion of <see cref="ShortBytes"/> to <see cref="UInt16"/>.
    /// </summary>
    [CLSCompliant(false)]
    public static implicit operator UInt16(ShortBytes value) => value.UShortValue;

    /// <summary>
    /// Defines an implicit conversion of <see cref="ShortBytes"/> to <see cref="Char"/>.
    /// </summary>
    [CLSCompliant(false)]
    public static implicit operator Char(ShortBytes value) => value.CharValue;
  }

  /// <summary>Union used for re-interpreting 32 bit numeric types as byte arrays and vice-versa.</summary>
  [StructLayout(LayoutKind.Explicit)]
  public struct IntBytes
  {
    /// <summary><c>Int32</c> version of the value.</summary>
    [FieldOffset(0)]
    public readonly Int32 IntValue;

    /// <summary><c>UInt32</c> version of the value.</summary>
    [FieldOffset(0), CLSCompliant(false)]
    public readonly UInt32 UIntValue;

    /// <summary><c>Single</c> version of the value.</summary>
    [FieldOffset(0)]
    public readonly Single SingleValue;

    [FieldOffset(0)]
    Byte byte0;

    /// <summary><c>Byte 0</c> of the value.</summary>
    public Byte Byte0 { get { return byte0; } }

    [FieldOffset(1)]
    Byte byte1;

    /// <summary><c>Byte 1</c> of the value.</summary>
    public Byte Byte1 { get { return byte1; } }

    [FieldOffset(2)]
    Byte byte2;

    /// <summary><c>Byte 2</c> of the value.</summary>
    public Byte Byte2 { get { return byte2; } }

    [FieldOffset(3)]
    Byte byte3;

    /// <summary><c>Byte 3</c> of the value.</summary>
    public Byte Byte3 { get { return byte3; } }

    /// <summary>Constructor taking an <c>Int32</c> value.</summary>
    public IntBytes(Int32 value) {
      this = new IntBytes();
      IntValue = value;
    }

    /// <summary>Constructor taking an <c>UInt32</c> value.</summary>
    [CLSCompliant(false)]
    public IntBytes(UInt32 value) {
      this = new IntBytes();
      UIntValue = value;
    }

    /// <summary>Constructor taking a <c>Single</c> value.</summary>
    public IntBytes(Single value) {
      this = new IntBytes();
      SingleValue = value;
    }

    /// <summary>Returns copy with reversed byte order.</summary>
    public IntBytes ReverseByteOrder() {
      var result = new IntBytes();
      result.byte0 = byte3;
      result.byte1 = byte2;
      result.byte2 = byte1;
      result.byte3 = byte0;
      return result;
    }

    /// <summary>Returns new instance initialized from <c>byte array</c>.</summary>
    /// <param name="value">Byte array to initialize the instance with.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public static IntBytes FromBytes(ReadOnlySpan<byte> value, ref int index) {
      IntBytes result = new IntBytes();
      int i = index;
      result.byte0 = value[i++];
      result.byte1 = value[i++];
      result.byte2 = value[i++];
      result.byte3 = value[i++];
      index = i;
      return result;
    }

    /// <summary>Returns new instance initialized from reversed <c>byte array</c>.</summary>
    /// <param name="value">Byte array to initialize the instance with.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public static IntBytes FromBytesReversed(ReadOnlySpan<byte> value, ref int index) {
      IntBytes result = new IntBytes();
      int i = index;
      result.byte3 = value[i++];
      result.byte2 = value[i++];
      result.byte1 = value[i++];
      result.byte0 = value[i++];
      index = i;
      return result;
    }

    /// <summary>Writes the value to a byte array.</summary>
    /// <param name="bytes">Target byte array.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public void ToBytes(Span<byte> bytes, ref int index) {
      int i = index;
      bytes[i++] = byte0;
      bytes[i++] = byte1;
      bytes[i++] = byte2;
      bytes[i++] = byte3;
      index = i;
    }

    /// <summary>Writes the value to a byte array in reversed byte order.</summary>
    /// <param name="bytes">Target byte array.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public void ToBytesReversed(Span<byte> bytes, ref int index) {
      int i = index;
      bytes[i++] = byte3;
      bytes[i++] = byte2;
      bytes[i++] = byte1;
      bytes[i++] = byte0;
      index = i;
    }

    /// <summary>
    /// Defines an implicit conversion of <see cref="Int32"/> to <see cref="IntBytes"/>.
    /// </summary>
    public static implicit operator IntBytes(Int32 value) => new IntBytes(value);

    /// <summary>
    /// Defines an implicit conversion of <see cref="UInt32"/> to <see cref="IntBytes"/>.
    /// </summary>
    [CLSCompliant(false)]
    public static implicit operator IntBytes(UInt32 value) => new IntBytes(value);

    /// <summary>
    /// Defines an implicit conversion of <see cref="Single"/> to <see cref="IntBytes"/>.
    /// </summary>
    [CLSCompliant(false)]
    public static implicit operator IntBytes(Single value) => new IntBytes(value);

    /// <summary>
    /// Defines an implicit conversion of <see cref="IntBytes"/> to <see cref="Int32"/>.
    /// </summary>
    public static implicit operator Int32(IntBytes value) => value.IntValue;

    /// <summary>
    /// Defines an implicit conversion of <see cref="IntBytes"/> to <see cref="UInt32"/>.
    /// </summary>
    [CLSCompliant(false)]
    public static implicit operator UInt32(IntBytes value) => value.UIntValue;

    /// <summary>
    /// Defines an implicit conversion of <see cref="IntBytes"/> to <see cref="Single"/>.
    /// </summary>
    [CLSCompliant(false)]
    public static implicit operator Single(IntBytes value) => value.SingleValue;
  }

  /// <summary>Union used for re-interpreting 64 bit numeric types as byte arrays and vice-versa.</summary>
  [StructLayout(LayoutKind.Explicit)]
  public struct LongBytes
  {
    /// <summary><c>Int64</c> version of the value.</summary>
    [FieldOffset(0)]
    public readonly Int64 LongValue;

    /// <summary><c>UInt64</c> version of the value.</summary>
    [FieldOffset(0), CLSCompliant(false)]
    public readonly UInt64 ULongValue;

    /// <summary><c>Double</c> version of the value.</summary>
    [FieldOffset(0)]
    public readonly Double DoubleValue;

    [FieldOffset(0)]
    byte byte0;

    /// <summary><c>Byte 0</c> of the value.</summary>
    public Byte Byte0 { get { return byte0; } }

    [FieldOffset(1)]
    byte byte1;

    /// <summary><c>Byte 1</c> of the value.</summary>
    public Byte Byte1 { get { return byte1; } }

    [FieldOffset(2)]
    byte byte2;

    /// <summary><c>Byte 2</c> of the value.</summary>
    public Byte Byte2 { get { return byte2; } }

    [FieldOffset(3)]
    byte byte3;

    /// <summary><c>Byte 3</c> of the value.</summary>
    public Byte Byte3 { get { return byte3; } }

    [FieldOffset(4)]
    byte byte4;

    /// <summary><c>Byte 4</c> of the value.</summary>
    public Byte Byte4 { get { return byte4; } }

    [FieldOffset(5)]
    byte byte5;

    /// <summary><c>Byte 5</c> of the value.</summary>
    public Byte Byte5 { get { return byte5; } }

    [FieldOffset(6)]
    byte byte6;

    /// <summary><c>Byte 6</c> of the value.</summary>
    public Byte Byte6 { get { return byte6; } }

    [FieldOffset(7)]
    byte byte7;

    /// <summary><c>Byte 7</c> of the value.</summary>
    public Byte Byte7 { get { return byte7; } }

    /// <summary>Constructor taking an <c>Int64</c> value.</summary>
    public LongBytes(Int64 value) {
      this = new LongBytes();
      LongValue = value;
    }

    /// <summary>Constructor taking an <c>UInt64</c> value.</summary>
    [CLSCompliant(false)]
    public LongBytes(UInt64 value) {
      this = new LongBytes();
      ULongValue = value;
    }

    /// <summary>Constructor taking a <c>Double</c> value.</summary>
    public LongBytes(Double value) {
      this = new LongBytes();
      DoubleValue = value;
    }

    /// <summary>Returns copy with reversed byte order.</summary>
    public LongBytes ReverseByteOrder() {
      var result = new LongBytes();
      result.byte0 = byte7;
      result.byte1 = byte6;
      result.byte2 = byte5;
      result.byte3 = byte4;
      result.byte4 = byte3;
      result.byte5 = byte2;
      result.byte6 = byte1;
      result.byte7 = byte0;
      return result;
    }

    /// <summary>Returns new instance initialized from <c>byte array</c>.</summary>
    /// <param name="value">Byte array to initialize the instance with.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public static LongBytes FromBytes(ReadOnlySpan<byte> value, ref int index) {
      LongBytes result = new LongBytes();
      int i = index;
      result.byte0 = value[i++];
      result.byte1 = value[i++];
      result.byte2 = value[i++];
      result.byte3 = value[i++];
      result.byte4 = value[i++];
      result.byte5 = value[i++];
      result.byte6 = value[i++];
      result.byte7 = value[i++];
      index = i;
      return result;
    }

    /// <summary>Returns new instance initialized from reversed <c>byte array</c>.</summary>
    /// <param name="value">Byte array to initialize the instance with.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public static LongBytes FromBytesReversed(ReadOnlySpan<byte> value, ref int index) {
      LongBytes result = new LongBytes();
      int i = index;
      result.byte7 = value[i++];
      result.byte6 = value[i++];
      result.byte5 = value[i++];
      result.byte4 = value[i++];
      result.byte3 = value[i++];
      result.byte2 = value[i++];
      result.byte1 = value[i++];
      result.byte0 = value[i++];
      index = i;
      return result;
    }

    /// <summary>Writes the value to a byte array.</summary>
    /// <param name="bytes">Target byte array.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public void ToBytes(Span<byte> bytes, ref int index) {
      int i = index;
      bytes[i++] = byte0;
      bytes[i++] = byte1;
      bytes[i++] = byte2;
      bytes[i++] = byte3;
      bytes[i++] = byte4;
      bytes[i++] = byte5;
      bytes[i++] = byte6;
      bytes[i++] = byte7;
      index = i;
    }

    /// <summary>Writes the value to a byte array in reversed byte order.</summary>
    /// <param name="bytes">Target byte array.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public void ToBytesReversed(Span<byte> bytes, ref int index) {
      int i = index;
      bytes[i++] = byte7;
      bytes[i++] = byte6;
      bytes[i++] = byte5;
      bytes[i++] = byte4;
      bytes[i++] = byte3;
      bytes[i++] = byte2;
      bytes[i++] = byte1;
      bytes[i++] = byte0;
      index = i;
    }

    /// <summary>
    /// Defines an implicit conversion of <see cref="Int64"/> to <see cref="LongBytes"/>.
    /// </summary>
    public static implicit operator LongBytes(Int64 value) => new LongBytes(value);

    /// <summary>
    /// Defines an implicit conversion of <see cref="UInt64"/> to <see cref="LongBytes"/>.
    /// </summary>
    [CLSCompliant(false)]
    public static implicit operator LongBytes(UInt64 value) => new LongBytes(value);

    /// <summary>
    /// Defines an implicit conversion of <see cref="Double"/> to <see cref="LongBytes"/>.
    /// </summary>
    [CLSCompliant(false)]
    public static implicit operator LongBytes(Double value) => new LongBytes(value);

    /// <summary>
    /// Defines an implicit conversion of <see cref="LongBytes"/> to <see cref="Int64"/>.
    /// </summary>
    public static implicit operator Int64(LongBytes value) => value.LongValue;

    /// <summary>
    /// Defines an implicit conversion of <see cref="LongBytes"/> to <see cref="UInt64"/>.
    /// </summary>
    [CLSCompliant(false)]
    public static implicit operator UInt64(LongBytes value) => value.ULongValue;

    /// <summary>
    /// Defines an implicit conversion of <see cref="LongBytes"/> to <see cref="Double"/>.
    /// </summary>
    [CLSCompliant(false)]
    public static implicit operator Double(LongBytes value) => value.DoubleValue;
  }

  /// <summary>Union used solely for re-interpreting <c>UInt16[]</c> as <c>Int16[]</c> or <c>Char[]</c> and vice versa.</summary>
  [StructLayout(LayoutKind.Explicit)]
  public struct ShortArrayUnion
  {
    /// <summary><c>UInt16[]</c> version of the value.</summary>
    [FieldOffset(0), CLSCompliant(false)]
    public readonly UInt16[] UShortValue;

    /// <summary><c>Int16[]</c> version of the value.</summary>
    [FieldOffset(0)]
    public readonly Int16[] ShortValue;

    /// <summary><c>Char[]</c> version of the value.</summary>
    [FieldOffset(0)]
    public readonly Char[] CharValue;

    /// <summary>Constructor taking an <c>UInt16[]</c> value.</summary>
    [CLSCompliant(false)]
    public ShortArrayUnion(UInt16[] value) {
      this = new ShortArrayUnion();  // keeps the compiler happy
      UShortValue = value;
    }

    /// <summary>Constructor taking an <c>Int16[]</c> value.</summary>
    public ShortArrayUnion(Int16[] value) {
      this = new ShortArrayUnion();  // keeps the compiler happy
      ShortValue = value;
    }

    /// <summary>Constructor taking a <c>Char[]</c> value.</summary>
    public ShortArrayUnion(Char[] value) {
      this = new ShortArrayUnion();  // keeps the compiler happy
      CharValue = value;
    }
  }

  /// <summary>Union used solely for re-interpreting <c>UInt32[]</c> as <c>Int32[]</c> and vice versa.</summary>
  [StructLayout(LayoutKind.Explicit)]
  public struct IntArrayUnion
  {
    /// <summary><c>UInt32[]</c> version of the value.</summary>
    [FieldOffset(0), CLSCompliant(false)]
    public readonly UInt32[] UIntValue;

    /// <summary><c>Int32[]</c> version of the value.</summary>
    [FieldOffset(0)]
    public readonly Int32[] IntValue;

    /// <summary>Constructor taking a <c>UInt32[]</c> value.</summary>
    [CLSCompliant(false)]
    public IntArrayUnion(UInt32[] value) {
      IntValue = null;  // keeps the compiler happy
      UIntValue = value;
    }

    /// <summary>Constructor taking an <c>Int32[]</c> value.</summary>
    public IntArrayUnion(Int32[] value) {
      UIntValue = null;  // keeps the compiler happy
      IntValue = value;
    }
  }

  /// <summary>
  /// Union used for re-interpreting a decimal as four integers and vice-versa.
  /// This works best for dealling with differences in endian-ness, since they
  /// apply only at the level of the Int32 parts. The overall storage order
  /// of these Int32 components is always: Lo->Mid->Hi->Flags.
  /// </summary>
  [StructLayout(LayoutKind.Explicit)]
  public struct DecimalBytes
  {
    /// <summary><c>Int16</c> version of the value.</summary>
    [FieldOffset(0)]
    public readonly Decimal DecimalValue;

    /// <summary>First <c>UInt64</c> part of the value.</summary>
    [FieldOffset(0)]
    public readonly Int32 Lo;

    /// <summary>Second <c>UInt64</c> part of the value.</summary>
    [FieldOffset(sizeof(Int32))]
    public readonly Int32 Mid;

    [FieldOffset(2 * sizeof(Int32))]
    public readonly Int32 Hi;

    [FieldOffset(3 * sizeof(Int32))]
    public readonly Int32 Flags;

    /// <summary>Constructor taking a <c>Decimal</c> value.</summary>
    public DecimalBytes(Decimal value) {
      this = new DecimalBytes();
      DecimalValue = value;
    }

    /// <summary>Constructor taking two <c>UInt64</c> values.</summary>
    public DecimalBytes(Int32 lo, Int32 mid, Int32 hi, Int32 flags) {
      this = new DecimalBytes();
      Lo = lo;
      Mid = mid;
      Hi = hi;
      Flags = flags;
    }

    /// <summary>Constructor taking two <c>Int64</c> values.</summary>
    [CLSCompliant(false)]
    public DecimalBytes(UInt32 lo, UInt32 mid, UInt32 hi, UInt32 flags) :this(
      unchecked((Int32)lo),
      unchecked((Int32)mid),
      unchecked((Int32)hi),
      unchecked((Int32)flags)
    ) { }

    /// <summary>Returns copy where the Int32 components have a reversed byte order,
    /// but the overall order of the components stays the same.</summary>
    public DecimalBytes ReverseByteOrder() {
      return new DecimalBytes(
        new IntBytes(Lo).ReverseByteOrder(),
        new IntBytes(Mid).ReverseByteOrder(),
        new IntBytes(Hi).ReverseByteOrder(),
        new IntBytes(Flags).ReverseByteOrder()
      );
    }

    /// <summary>
    /// Defines an implicit conversion of <see cref="Decimal"/> to <see cref="DecimalBytes"/>.
    /// </summary>
    public static implicit operator DecimalBytes(Decimal value) => new DecimalBytes(value);

    /// <summary>
    /// Defines an implicit conversion of <see cref="DecimalBytes"/> to <see cref="Decimal"/>.
    /// </summary>
    public static implicit operator Decimal(DecimalBytes value) => value.DecimalValue;
  }

  /// <summary>Union used for re-interpreting a decimal as two unsigned longs and vice-versa.
  /// This is only useful for to re-using the serialization implementation of unsigned long.</summary>
  [StructLayout(LayoutKind.Explicit)]
  internal struct DecimalLongUnion
  {
    /// <summary><c>Int16</c> version of the value.</summary>
    [FieldOffset(0)]
    public readonly Decimal DecimalValue;

    /// <summary>First <c>UInt64</c> part of the value.</summary>
    [FieldOffset(0), CLSCompliant(false)]
    public readonly UInt64 Long0;

    /// <summary>Second <c>UInt64</c> part of the value.</summary>
    [FieldOffset(sizeof(ulong)), CLSCompliant(false)]
    public readonly UInt64 Long1;

    /// <summary>Constructor taking a <c>Decimal</c> value.</summary>
    public DecimalLongUnion(Decimal value) {
      this = new DecimalLongUnion();
      DecimalValue = value;
    }

    /// <summary>Constructor taking a <c>DecimalBytes</c> value.</summary>
    public DecimalLongUnion(DecimalBytes value) {
      this = new DecimalLongUnion();
      DecimalValue = value.DecimalValue;
    }

    /// <summary>Constructor taking two <c>UInt64</c> values.</summary>
    [CLSCompliant(false)]
    public DecimalLongUnion(UInt64 long0, UInt64 long1) {
      this = new DecimalLongUnion();
      Long0 = long0;
      Long1 = long1;
    }

    /// <summary>Constructor taking two <c>Int64</c> values.</summary>
    [CLSCompliant(false)]
    public DecimalLongUnion(Int64 long0, Int64 long1):this(unchecked((UInt64)long0), unchecked((UInt64)long1)) { }
  }

  /// <summary>Serializes and deserializes numeric types in a specified byte order.</summary>
  public class ByteConverter {
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

    public void WriteBytes<T>(in T value, Span<byte> bytes, ref int index) where T : struct {
      if (!BufferHelpers.TryWriteBytes(value, bytes, ref index))
        throw new SerializationException("Buffer exhausted.");
    }

    public void WriteBytes(UInt16 value, Span<byte> bytes, ref int index) {
      if (reverse)
        value = value.ReverseByteOrder();
      WriteBytes<UInt16>(value, bytes, ref index);
    }

    public void WriteBytes(Int16 value, Span<byte> bytes, ref int index) {
      WriteBytes<Int16>(value, bytes, ref index);
    }

    public void WriteBytes(Char value, Span<byte> bytes, ref int index) {
      WriteBytes<Char>(value, bytes, ref index);
    }

    public void WriteBytes(UInt32 value, Span<byte> bytes, ref int index) {
      if (reverse)
        value = value.ReverseByteOrder();
      WriteBytes<UInt32>(value, bytes, ref index);
    }

    public void WriteBytes(Int32 value, Span<byte> bytes, ref int index) {
      WriteBytes<Int32>(value, bytes, ref index);
    }

    public void WriteBytes(Single value, Span<byte> bytes, ref int index) {
      if (reverse)
        value = value.ReverseByteOrder();
      WriteBytes<Single>(value, bytes, ref index);
    }

    public void WriteBytes(UInt64 value, Span<byte> bytes, ref int index) {
      if (reverse)
        value = value.ReverseByteOrder();
      WriteBytes<UInt64>(value, bytes, ref index);
    }

    public void WriteBytes(Int64 value, Span<byte> bytes, ref int index) {
      WriteBytes<Int64>(value, bytes, ref index);
    }

    public void WriteBytes(Double value, Span<byte> bytes, ref int index) {
      if (reverse)
        value = value.ReverseByteOrder();
      WriteBytes<Double>(value, bytes, ref index);
    }

    public void WriteBytes(Decimal value, Span<byte> bytes, ref int index) {
      if (reverse)
        value = value.ReverseByteOrder();
      WriteBytes<Decimal>(value, bytes, ref index);
    }

    #endregion

    #region Span WriteBytes

    public void WriteBytes<T>(ReadOnlySpan<T> values, Span<byte> bytes, ref int index) where T : struct {
      if (!BufferHelpers.TryWriteBytes<T>(values, bytes, ref index))
        throw new SerializationException("Buffer exhausted.");
    }

    public void WriteBytes<T>(ReadOnlySpan<T> values, Span<byte> bytes, ref int index, Func<T, T> beforeWrite) where T : struct {
      if (!BufferHelpers.TryWriteBytes<T>(values, bytes, ref index, beforeWrite))
        throw new SerializationException("Buffer exhausted.");
    }

    public void WriteBytes(ReadOnlySpan<UInt16> values, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteBytes(values, bytes, ref index, BufferHelpers.ReverseByteOrder);
      else
        WriteBytes(values, bytes, ref index);
    }

    public void WriteBytes(ReadOnlySpan<Int16> values, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteBytes(values, bytes, ref index, BufferHelpers.ReverseByteOrder);
      else
        WriteBytes(values, bytes, ref index);
    }

    public void WriteBytes(ReadOnlySpan<Char> values, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteBytes(values, bytes, ref index, BufferHelpers.ReverseByteOrder);
      else
        WriteBytes(values, bytes, ref index);
    }

    public void WriteBytes(ReadOnlySpan<UInt32> values, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteBytes(values, bytes, ref index, BufferHelpers.ReverseByteOrder);
      else
        WriteBytes(values, bytes, ref index);
    }

    public void WriteBytes(ReadOnlySpan<Int32> value, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteBytes(value, bytes, ref index, BufferHelpers.ReverseByteOrder);
      else
        WriteBytes(value, bytes, ref index);
    }

    public void WriteBytes(ReadOnlySpan<Single> values, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteBytes(values, bytes, ref index, BufferHelpers.ReverseByteOrder);
      else
        WriteBytes(values, bytes, ref index);
    }

    public void WriteBytes(ReadOnlySpan<UInt64> values, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteBytes(values, bytes, ref index, BufferHelpers.ReverseByteOrder);
      else
        WriteBytes(values, bytes, ref index);
    }

    public void WriteBytes(ReadOnlySpan<Int64> values, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteBytes(values, bytes, ref index, BufferHelpers.ReverseByteOrder);
      else
        WriteBytes(values, bytes, ref index);
    }

    public void WriteBytes(ReadOnlySpan<Double> values, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteBytes(values, bytes, ref index, BufferHelpers.ReverseByteOrder);
      else
        WriteBytes(values, bytes, ref index);
    }

    public void WriteBytes(ReadOnlySpan<Decimal> values, Span<byte> bytes, ref int index) {
      if (reverse)
        WriteBytes(values, bytes, ref index, BufferHelpers.ReverseByteOrder);
      else
        WriteBytes(values, bytes, ref index);
    }

    #endregion

    #region ReadBytes

    public void ReadBytes<T>(ReadOnlySpan<byte> bytes, ref int index, out T value) where T : struct {
      value = default;
      if (!BufferHelpers.TryReadBytes(bytes, ref index, ref value))
        throw new SerializationException("Buffer exhausted.");
    }

    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out UInt16 value) {
      ReadBytes<UInt16>(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out Int16 value) {
      ReadBytes<Int16>(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out Char value) {
      ReadBytes<Char>(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out UInt32 value) {
      ReadBytes<UInt32>(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out Int32 value) {
      ReadBytes<Int32>(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out Single value) {
      ReadBytes<Single>(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out UInt64 value) {
      ReadBytes<UInt64>(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out Int64 value) {
      ReadBytes<Int64>(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out Double value) {
      ReadBytes<Double>(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    public void ReadBytes(ReadOnlySpan<byte> bytes, ref int index, out Decimal value) {
      ReadBytes<Decimal>(bytes, ref index, out value);
      if (reverse)
        value = value.ReverseByteOrder();
    }

    #endregion

    #region Span ReadBytes

    public void ReadBytes<T>(ReadOnlySpan<byte> bytes, ref int index, Span<T> values) where T : struct {
      if (!BufferHelpers.TryReadBytes(bytes, ref index, values))
        throw new SerializationException("Buffer exhausted.");
    }

    public void ReadBytes(Span<byte> bytes, ref int index, Span<UInt16> values) {
      ReadBytes<UInt16>(bytes, ref index, values);
      if (reverse)
        BufferHelpers.ReverseByteOrder(values);
    }

    public void ReadBytes(Span<byte> bytes, ref int index, Span<Int16> values) {
      ReadBytes<Int16>(bytes, ref index, values);
      if (reverse)
        BufferHelpers.ReverseByteOrder(values);
    }

    public void ReadBytes(Span<byte> bytes, ref int index, Span<Char> values) {
      ReadBytes<Char>(bytes, ref index, values);
      if (reverse)
        BufferHelpers.ReverseByteOrder(values);
    }

    public void ReadBytes(Span<byte> bytes, ref int index, Span<UInt32> values) {
      ReadBytes<UInt32>(bytes, ref index, values);
      if (reverse)
        BufferHelpers.ReverseByteOrder(values);
    }

    public void ReadBytes(Span<byte> bytes, ref int index, Span<Int32> values) {
      ReadBytes<Int32>(bytes, ref index, values);
      if (reverse)
        BufferHelpers.ReverseByteOrder(values);
    }

    public void ReadBytes(Span<byte> bytes, ref int index, Span<Single> values) {
      ReadBytes<Single>(bytes, ref index, values);
      if (reverse)
        BufferHelpers.ReverseByteOrder(values);
    }

    public void ReadBytes(Span<byte> bytes, ref int index, Span<UInt64> values) {
      ReadBytes<UInt64>(bytes, ref index, values);
      if (reverse)
        BufferHelpers.ReverseByteOrder(values);
    }

    public void ReadBytes(Span<byte> bytes, ref int index, Span<Int64> values) {
      ReadBytes<Int64>(bytes, ref index, values);
      if (reverse)
        BufferHelpers.ReverseByteOrder(values);
    }

    public void ReadBytes(Span<byte> bytes, ref int index, Span<Double> values) {
      ReadBytes<Double>(bytes, ref index, values);
      if (reverse)
        BufferHelpers.ReverseByteOrder(values);
    }

    public void ReadBytes(Span<byte> bytes, ref int index, Span<Decimal> values) {
      ReadBytes<Decimal>(bytes, ref index, values);
      if (reverse)
        BufferHelpers.ReverseByteOrder(values);
    }

    #endregion
  }
}
