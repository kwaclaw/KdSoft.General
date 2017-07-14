/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2006, 2008
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
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

    /// <summary>Returns new instance initialized from <c>byte array</c>.</summary>
    /// <param name="value">Byte array to initialize the instance with.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public static ShortBytes FromBytes(byte[] value, ref int index) {
      ShortBytes result = new ShortBytes();
      result.byte0 = value[index++];
      result.byte1 = value[index++];
      return result;
    }

    /// <summary>Returns new instance initialized from reversed <c>byte array</c>.</summary>
    /// <param name="value">Byte array to initialize the instance with.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public static ShortBytes FromBytesReversed(byte[] value, ref int index) {
      ShortBytes result = new ShortBytes();
      result.byte1 = value[index++];
      result.byte0 = value[index++];
      return result;
    }

    /// <summary>Writes the value to a byte array.</summary>
    /// <param name="value">Target byte array.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public void ToBytes(byte[] bytes, ref int index) {
      bytes[index++] = byte0;
      bytes[index++] = byte1;
    }

    /// <summary>Writes the value to a byte array in reversed byte order.</summary>
    /// <param name="value">Target byte array.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public void ToBytesReversed(byte[] bytes, ref int index) {
      bytes[index++] = byte1;
      bytes[index++] = byte0;
    }
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

    /// <summary>Returns new instance initialized from <c>byte array</c>.</summary>
    /// <param name="value">Byte array to initialize the instance with.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public static IntBytes FromBytes(byte[] value, ref int index) {
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
    public static IntBytes FromBytesReversed(byte[] value, ref int index) {
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
    /// <param name="value">Target byte array.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public void ToBytes(byte[] bytes, ref int index) {
      int i = index;
      bytes[i++] = byte0;
      bytes[i++] = byte1;
      bytes[i++] = byte2;
      bytes[i++] = byte3;
      index = i;
    }

    /// <summary>Writes the value to a byte array in reversed byte order.</summary>
    /// <param name="value">Target byte array.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public void ToBytesReversed(byte[] bytes, ref int index) {
      int i = index;
      bytes[i++] = byte3;
      bytes[i++] = byte2;
      bytes[i++] = byte1;
      bytes[i++] = byte0;
      index = i;
    }
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

    /// <summary>Returns new instance initialized from <c>byte array</c>.</summary>
    /// <param name="value">Byte array to initialize the instance with.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public static LongBytes FromBytes(byte[] value, ref int index) {
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
    public static LongBytes FromBytesReversed(byte[] value, ref int index) {
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
    /// <param name="value">Target byte array.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public void ToBytes(byte[] bytes, ref int index) {
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
    /// <param name="value">Target byte array.</param>
    /// <param name="index">Starting index in byte array argument.</param>
    public void ToBytesReversed(byte[] bytes, ref int index) {
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

  /// <summary>Union used for re-interpreting a decimal as two unsigned longs and vice-versa.</summary>
  [StructLayout(LayoutKind.Explicit)]
  public struct DecimalLongUnion
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

  /// <summary>Serializes and deserialized numeric types in a specified byte order.</summary>
  public class ByteConverter
  {
    public static readonly ByteOrder SystemByteOrder;

    static ByteConverter() {
      SystemByteOrder = BitConverter.IsLittleEndian ? ByteOrder.LittleEndian : ByteOrder.BigEndian;
    }

    bool reverse;

    public bool Reverse {
      get { return reverse; }
    }

    public ByteConverter(ByteOrder byteOrder) {
      reverse = byteOrder != SystemByteOrder;
    }

    public ByteConverter(bool reverse = false) {
      this.reverse = reverse;
    }

    #region Not CLS-Compliant

    /// <summary>Serializes <c>UInt16</c> values.</summary>
    /// <param name="num">Value to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    [CLSCompliant(false)]
    public void ToBytes(UInt16 num, byte[] bytes, ref int index) {
      ShortBytes valBytes = new ShortBytes(num);
      if (reverse) {
        valBytes.ToBytesReversed(bytes, ref index);
      }
      else {
        valBytes.ToBytes(bytes, ref index);
      }
    }

    /// <summary>Deserializes <c>UInt16</c> values.</summary>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    /// <returns>Deserialized <c>UInt16</c> value.</returns>
    [CLSCompliant(false)]
    public UInt16 ToUInt16(byte[] bytes, ref int index) {
      ShortBytes valBytes;
      if (reverse) {
        valBytes = ShortBytes.FromBytesReversed(bytes, ref index);
      }
      else {
        valBytes = ShortBytes.FromBytes(bytes, ref index);
      }
      return valBytes.UShortValue;
    }

    /// <summary>Serializes <c>UInt16</c> array.</summary>
    /// <param name="numArray">Array to serialize.</param>
    /// <param name="start">Array index to start serializing at.</param>
    /// <param name="count">Number of array elements to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    [CLSCompliant(false)]
    public void
    ToBytes(UInt16[] numArray, int start, int count, byte[] bytes, ref int index) {
      int end = start + count;
      int byteIndex = index;
      ShortBytes valBytes;
      if (reverse) {
        for (; start != end; start++) {
          valBytes = new ShortBytes(numArray[start]);
          valBytes.ToBytesReversed(bytes, ref byteIndex);
        }
      }
      else {
        for (; start != end; start++) {
          valBytes = new ShortBytes(numArray[start]);
          valBytes.ToBytes(bytes, ref byteIndex);
        }
      }
      index = byteIndex;
    }

    /// <summary>Deserializes <c>UInt16</c> array.</summary>
    /// <param name="numArray">Pre-allocated array to fill with deserialized values.</param>
    /// <param name="start">Array index to start writing at.</param>
    /// <param name="count">Number of array elements to deserialize.</param>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    [CLSCompliant(false)]
    public void
    ToUInt16Array(UInt16[] numArray, int start, int count, byte[] bytes, ref int index) {
      int end = start + count;
      int byteIndex = index;
      ShortBytes valBytes;
      if (reverse) {
        for (; start != end; start++) {
          valBytes = ShortBytes.FromBytesReversed(bytes, ref byteIndex);
          numArray[start] = valBytes.UShortValue;
        }
      }
      else {
        for (; start != end; start++) {
          valBytes = ShortBytes.FromBytes(bytes, ref byteIndex);
          numArray[start] = valBytes.UShortValue;
        }
      }
      index = byteIndex;
    }

    /// <summary>Serializes <c>UInt32</c> values.</summary>
    /// <remarks>Use <c>unchecked((UInt32)&lt;argument>)</c> to pass signed values.</remarks>
    /// <param name="num">Value to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    [CLSCompliant(false)]
    public void ToBytes(UInt32 num, byte[] bytes, ref int index) {
      IntBytes valBytes = new IntBytes(num);
      if (reverse)
        valBytes.ToBytesReversed(bytes, ref index);
      else
        valBytes.ToBytes(bytes, ref index);
    }

    /// <summary>Deserializes <c>UInt32</c> values.</summary>
    /// <remarks>Use <c>unchecked((Int32)ToUInt32(&lt;bytes>, ref &lt;index>))</c>
    /// to return signed results.</remarks>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    /// <returns>Deserialized <c>UInt32</c> value.</returns>
    [CLSCompliant(false)]
    public UInt32 ToUInt32(byte[] bytes, ref int index) {
      IntBytes valBytes;
      if (reverse)
        valBytes = IntBytes.FromBytesReversed(bytes, ref index);
      else
        valBytes = IntBytes.FromBytes(bytes, ref index);
      return valBytes.UIntValue;
    }

    /// <summary>Serializes <c>UInt32</c> array.</summary>
    /// <param name="numArray">Array to serialize.</param>
    /// <param name="start">Array index to start serializing at.</param>
    /// <param name="count">Number of array elements to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    [CLSCompliant(false)]
    public void
    ToBytes(UInt32[] numArray, int start, int count, byte[] bytes, ref int index) {
      int end = start + count;
      int byteIndex = index;
      IntBytes valBytes;
      if (reverse) {
        for (; start != end; start++) {
          valBytes = new IntBytes(numArray[start]);
          valBytes.ToBytesReversed(bytes, ref byteIndex);
        }
      }
      else {
        for (; start != end; start++) {
          valBytes = new IntBytes(numArray[start]);
          valBytes.ToBytes(bytes, ref byteIndex);
        }
      }
      index = byteIndex;
    }

    /// <summary>Deserializes <c>UInt32</c> array.</summary>
    /// <param name="numArray">Pre-allocated array to fill with deserialized values.</param>
    /// <param name="start">Array index to start writing at.</param>
    /// <param name="count">Number of array elements to deserialize.</param>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    [CLSCompliant(false)]
    public void
    ToUInt32Array(UInt32[] numArray, int start, int count, byte[] bytes, ref int index) {
      int end = start + count;
      int byteIndex = index;
      IntBytes valBytes;
      if (reverse) {
        for (; start != end; start++) {
          valBytes = IntBytes.FromBytesReversed(bytes, ref byteIndex);
          numArray[start] = valBytes.UIntValue;
        }
      }
      else {
        for (; start != end; start++) {
          valBytes = IntBytes.FromBytes(bytes, ref byteIndex);
          numArray[start] = valBytes.UIntValue;
        }
      }
      index = byteIndex;
    }

    /// <summary>Serializes <c>UInt64</c> values.</summary>
    /// <remarks>Use <c>unchecked((UInt64)&lt;argument>)</c> to pass signed values.</remarks>
    /// <param name="num">Value to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    [CLSCompliant(false)]
    public void ToBytes(UInt64 num, byte[] bytes, ref int index) {
      LongBytes valBytes = new LongBytes(num);
      if (reverse)
        valBytes.ToBytesReversed(bytes, ref index);
      else
        valBytes.ToBytes(bytes, ref index);
    }

    /// <summary>Deserializes <c>UInt64</c> values.</summary>
    /// <remarks>Use <c>unchecked((Int64)ToUInt64(&lt;bytes>, ref &lt;index>))</c>
    /// to return signed results.</remarks>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    /// <returns>Deserialized <c>UInt64</c> value.</returns>
    [CLSCompliant(false)]
    public UInt64 ToUInt64(byte[] bytes, ref int index) {
      LongBytes valBytes;
      if (reverse)
        valBytes = LongBytes.FromBytesReversed(bytes, ref index);
      else
        valBytes = LongBytes.FromBytes(bytes, ref index);
      return valBytes.ULongValue;
    }

    #endregion

    #region CLS-Compliant

    /// <summary>Serializes <c>Int16</c> values.</summary>
    /// <param name="num">Value to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    public void ToBytes(Int16 num, byte[] bytes, ref int index) {
      unchecked { ToBytes((UInt16)num, bytes, ref index); }
    }

    /// <summary>Deserializes <c>Int16</c> values.</summary>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    /// <returns>Deserialized <c>Int16</c> value.</returns>
    public Int16 ToInt16(byte[] bytes, ref int index) {
      unchecked { return (Int16)ToUInt16(bytes, ref index); }
    }

    /// <summary>Serializes <c>Char</c> values.</summary>
    /// <param name="chr">Value to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    public void ToBytes(Char chr, byte[] bytes, ref int index) {
      unchecked { ToBytes((UInt16)chr, bytes, ref index); }
    }

    /// <summary>Deserializes <c>Char</c> values.</summary>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    /// <returns>Deserialized <c>Char</c> value.</returns>
    public Char ToChar(byte[] bytes, ref int index) {
      unchecked { return (Char)ToUInt16(bytes, ref index); }
    }

    /// <summary>Serializes <c>Int16</c> array.</summary>
    /// <param name="numArray">Array to serialize.</param>
    /// <param name="start">Array index to start serializing at.</param>
    /// <param name="count">Number of array elements to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    public void
    ToBytes(Int16[] numArray, int start, int count, byte[] bytes, ref int index) {
      ShortArrayUnion union = new ShortArrayUnion(numArray);
      ToBytes(union.UShortValue, start, count, bytes, ref index);
    }

    /// <summary>Deserializes <c>Int16</c> array.</summary>
    /// <param name="numArray">Pre-allocated array to fill with deserialized values.</param>
    /// <param name="start">Array index to start writing at.</param>
    /// <param name="count">Number of array elements to deserialize.</param>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    public void
    ToInt16Array(Int16[] numArray, int start, int count, byte[] bytes, ref int index) {
      ShortArrayUnion union = new ShortArrayUnion(numArray);
      ToUInt16Array(union.UShortValue, start, count, bytes, ref index);
    }

    /// <summary>Serializes <c>Char</c> array.</summary>
    /// <param name="charArray">Array to serialize.</param>
    /// <param name="start">Array index to start serializing at.</param>
    /// <param name="count">Number of array elements to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    public void
    ToBytes(Char[] charArray, int start, int count, byte[] bytes, ref int index) {
      ShortArrayUnion union = new ShortArrayUnion(charArray);
      ToBytes(union.UShortValue, start, count, bytes, ref index);
    }

    /// <summary>Deserializes <c>Char</c> array.</summary>
    /// <param name="charArray">Pre-allocated array to fill with deserialized values.</param>
    /// <param name="start">Array index to start writing at.</param>
    /// <param name="count">Number of array elements to deserialize.</param>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    public void
    ToCharArray(Char[] charArray, int start, int count, byte[] bytes, ref int index) {
      ShortArrayUnion union = new ShortArrayUnion(charArray);
      ToUInt16Array(union.UShortValue, start, count, bytes, ref index);
    }

    /// <summary>Serializes <c>Int32</c> values.</summary>
    /// <param name="num">Value to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    public void ToBytes(Int32 num, byte[] bytes, ref int index) {
      unchecked { ToBytes((UInt32)num, bytes, ref index); }
    }

    /// <summary>Deserializes <c>Int32</c> values.</summary>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    /// <returns>Deserialized <c>Int32</c> value.</returns>
    public Int32 ToInt32(byte[] bytes, ref int index) {
      unchecked { return (Int32)ToUInt32(bytes, ref index); }
    }

    /// <summary>Serializes <c>Int32</c> array.</summary>
    /// <param name="numArray">Array to serialize.</param>
    /// <param name="start">Array index to start serializing at.</param>
    /// <param name="count">Number of array elements to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    public void
    ToBytes(Int32[] numArray, int start, int count, byte[] bytes, ref int index) {
      IntArrayUnion union = new IntArrayUnion(numArray);
      ToBytes(union.UIntValue, start, count, bytes, ref index);
    }

    /// <summary>Deserializes <c>Int32</c> array.</summary>
    /// <param name="numArray">Pre-allocated array to fill with deserialized values.</param>
    /// <param name="start">Array index to start writing at.</param>
    /// <param name="count">Number of array elements to deserialize.</param>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    public void
    ToInt32Array(Int32[] numArray, int start, int count, byte[] bytes, ref int index) {
      IntArrayUnion union = new IntArrayUnion(numArray);
      ToUInt32Array(union.UIntValue, start, count, bytes, ref index);
    }

    /// <summary>Serializes <c>Int64</c> values.</summary>
    /// <param name="num">Value to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    public void ToBytes(Int64 num, byte[] bytes, ref int index) {
      unchecked { ToBytes((UInt64)num, bytes, ref index); }
    }

    /// <summary>Deserializes <c>Int64</c> values.</summary>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    /// <returns>Deserialized <c>Int64</c> value.</returns>
    public Int64 ToInt64(byte[] bytes, ref int index) {
      unchecked { return (Int64)ToUInt64(bytes, ref index); }
    }

    /// <summary>Serializes <c>Single</c> values.</summary>
    /// <param name="num">Value to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    public void ToBytes(Single num, byte[] bytes, ref int index) {
      IntBytes valBytes = new IntBytes(num);
      if (reverse)
        valBytes.ToBytesReversed(bytes, ref index);
      else
        valBytes.ToBytes(bytes, ref index);
    }

    /// <summary>Deserializes <c>Single</c> values.</summary>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    /// <returns>Deserialized <c>Single</c> value.</returns>
    public Single ToSingle(byte[] bytes, ref int index) {
      IntBytes valBytes;
      if (reverse)
        valBytes = IntBytes.FromBytesReversed(bytes, ref index);
      else
        valBytes = IntBytes.FromBytes(bytes, ref index);
      return valBytes.SingleValue;
    }

    /// <summary>Serializes <c>Double</c> values.</summary>
    /// <param name="num">Value to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    public void ToBytes(Double num, byte[] bytes, ref int index) {
      LongBytes valBytes = new LongBytes(num);
      if (reverse)
        valBytes.ToBytesReversed(bytes, ref index);
      else
        valBytes.ToBytes(bytes, ref index);
    }

    /// <summary>Deserializes <c>Double</c> values.</summary>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    /// <returns>Deserialized <c>Double</c> value.</returns>
    public Double ToDouble(byte[] bytes, ref int index) {
      LongBytes valBytes;
      if (reverse)
        valBytes = LongBytes.FromBytesReversed(bytes, ref index);
      else
        valBytes = LongBytes.FromBytes(bytes, ref index);
      return valBytes.DoubleValue;
    }

    /// <summary>Serializes <c>Decimal</c> values.</summary>
    /// <remarks>Processes a <c>Decimal</c> value in reverse part order, where
    /// each part is a <c>Int32</c> value. This is a .NET specific type.</remarks>
    /// <param name="num">Value to serialize.</param>
    /// <param name="bytes">Byte buffer to write to.</param>
    /// <param name="index">Byte index to start writing at.</param>
    public void ToBytes(Decimal num, byte[] bytes, ref int index) {
      DecimalLongUnion union = new DecimalLongUnion(num);
      if (reverse) {
        ToBytes(union.Long1, bytes, ref index);
        ToBytes(union.Long0, bytes, ref index);
      }
      else {
        ToBytes(union.Long0, bytes, ref index);
        ToBytes(union.Long1, bytes, ref index);
      }
    }

    /// <summary>Deserializes <c>Decimal</c> values.</summary>
    /// <remarks>Processes the buffer as four <c>Int32</c> values which form the
    /// parts of a <c>Decimal</c> value in reverse order. This is .NET specific.</remarks>
    /// <param name="bytes">Byte buffer to read from.</param>
    /// <param name="index">Byte index to start reading at.</param>
    /// <returns>Deserialized <c>Decimal</c> value.</returns>
    public Decimal ToDecimal(byte[] bytes, ref int index) {
      UInt64 long0, long1;
      if (reverse) {
        long1 = ToUInt64(bytes, ref index);
        long0 = ToUInt64(bytes, ref index);
      }
      else {
        long0 = ToUInt64(bytes, ref index);
        long1 = ToUInt64(bytes, ref index);
      }
      DecimalLongUnion union = new DecimalLongUnion(long0, long1);
      return union.DecimalValue;
    }

    #endregion

    #region Convenience Methods

    /// <inheritdoc/>
    [CLSCompliant(false)]
    public byte[] ToBytes(UInt16 value) {
      int index = 0;
      byte[] result = new byte[sizeof(UInt16)];
      ToBytes(value, result, ref index);
      return result;
    }

    /// <inheritdoc/>
    [CLSCompliant(false)]
    public byte[] ToBytes(UInt32 value) {
      int index = 0;
      byte[] result = new byte[sizeof(UInt32)];
      ToBytes(value, result, ref index);
      return result;
    }

    /// <inheritdoc/>
    [CLSCompliant(false)]
    public byte[] ToBytes(UInt64 value) {
      int index = 0;
      byte[] result = new byte[sizeof(UInt64)];
      ToBytes(value, result, ref index);
      return result;
    }

    /// <inheritdoc/>
    [CLSCompliant(false)]
    public byte[] ToBytes(UInt16[] value, int start = 0, int count = -1) {
      int index = 0;
      if (count == -1) count = value.Length;
      byte[] result = new byte[sizeof(UInt16) * count];
      ToBytes(value, start, count, result, ref index);
      return result;
    }

    /// <inheritdoc/>
    [CLSCompliant(false)]
    public byte[] ToBytes(UInt32[] value, int start = 0, int count = -1) {
      int index = 0;
      if (count == -1) count = value.Length;
      byte[] result = new byte[sizeof(UInt32) * count];
      ToBytes(value, start, count, result, ref index);
      return result;
    }

    /// <inheritdoc/>
    public byte[] ToBytes(Int16 value) {
      int index = 0;
      byte[] result = new byte[sizeof(Int16)];
      ToBytes(value, result, ref index);
      return result;
    }

    /// <inheritdoc/>
    public byte[] ToBytes(Char value) {
      int index = 0;
      byte[] result = new byte[sizeof(Char)];
      ToBytes(value, result, ref index);
      return result;
    }

    /// <inheritdoc/>
    public byte[] ToBytes(Int32 value) {
      int index = 0;
      byte[] result = new byte[sizeof(Int32)];
      ToBytes(value, result, ref index);
      return result;
    }

    /// <inheritdoc/>
    public byte[] ToBytes(Int64 value) {
      int index = 0;
      byte[] result = new byte[sizeof(Int64)];
      ToBytes(value, result, ref index);
      return result;
    }

    /// <inheritdoc/>
    public byte[] ToBytes(Single value) {
      int index = 0;
      byte[] result = new byte[sizeof(Single)];
      ToBytes(value, result, ref index);
      return result;
    }

    /// <inheritdoc/>
    public byte[] ToBytes(Double value) {
      int index = 0;
      byte[] result = new byte[sizeof(Double)];
      ToBytes(value, result, ref index);
      return result;
    }

    /// <inheritdoc/>
    public byte[] ToBytes(Decimal value) {
      int index = 0;
      byte[] result = new byte[sizeof(Decimal)];
      ToBytes(value, result, ref index);
      return result;
    }

    /// <inheritdoc/>
    public byte[] ToBytes(Int16[] value, int start = 0, int count = -1) {
      int index = 0;
      if (count == -1) count = value.Length;
      byte[] result = new byte[sizeof(Int16) * count];
      ToBytes(value, start, count, result, ref index);
      return result;
    }

    /// <inheritdoc/>
    public byte[] ToBytes(Char[] value, int start = 0, int count = -1) {
      int index = 0;
      if (count == -1) count = value.Length;
      byte[] result = new byte[sizeof(Char) * count];
      ToBytes(value, start, count, result, ref index);
      return result;
    }

    /// <inheritdoc/>
    public byte[] ToBytes(Int32[] value, int start = 0, int count = -1) {
      int index = 0;
      if (count == -1) count = value.Length;
      byte[] result = new byte[sizeof(Int32) * count];
      ToBytes(value, start, count, result, ref index);
      return result;
    }

    #endregion
  }
}
