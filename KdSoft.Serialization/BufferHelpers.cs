using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace KdSoft.Serialization.Buffer
{
  public static class BufferHelpers
  {

    #region Reverse Byte Order

    #region Integers, Chars, Floats

    /// <summary>Returns copy with reversed byte order.</summary>
    [CLSCompliant(false)]
    public static UInt16 ReverseByteOrder(this UInt16 value) {
      return ((ShortBytes)value).ReverseByteOrder();
    }

    /// <summary>Returns copy with reversed byte order.</summary>
    public static Int16 ReverseByteOrder(this Int16 value) {
      return ((ShortBytes)value).ReverseByteOrder();
    }

    /// <summary>Returns copy with reversed byte order.</summary>
    public static Char ReverseByteOrder(this Char value) {
      return ((ShortBytes)value).ReverseByteOrder();
    }

    /// <summary>Returns copy with reversed byte order.</summary>
    [CLSCompliant(false)]
    public static UInt32 ReverseByteOrder(this UInt32 value) {
      return ((IntBytes)value).ReverseByteOrder();
    }

    /// <summary>Returns copy with reversed byte order.</summary>
    public static Int32 ReverseByteOrder(this Int32 value) {
      return ((IntBytes)value).ReverseByteOrder();
    }

    /// <summary>Returns copy with reversed byte order.</summary>
    public static Single ReverseByteOrder(this Single value) {
      return ((IntBytes)value).ReverseByteOrder();
    }

    /// <summary>Returns copy with reversed byte order.</summary>
    [CLSCompliant(false)]
    public static UInt64 ReverseByteOrder(this UInt64 value) {
      return ((LongBytes)value).ReverseByteOrder();
    }

    /// <summary>Returns copy with reversed byte order.</summary>
    public static Int64 ReverseByteOrder(this Int64 value) {
      return ((LongBytes)value).ReverseByteOrder();
    }

    /// <summary>Returns copy with reversed byte order.</summary>
    public static Double ReverseByteOrder(this Double value) {
      return ((LongBytes)value).ReverseByteOrder();
    }

    /// <summary>Returns copy where the individual Int32 components have a reversed byte order.
    /// Note: The order of the components itself is not reversed.</summary>
    public static Decimal ReverseByteOrder(this Decimal value) {
      return ((DecimalBytes)value).ReverseByteOrder();
    }

    #endregion

    #region Spans of Integers, Chars, Floats

    /// <summary>Reverses the byte order of all elements in the Span.</summary>
    [CLSCompliant(false)]
    public static void ReverseByteOrder(Span<UInt16> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = ReverseByteOrder(value);
      }
    }

    /// <summary>Reverses the byte order of all elements in the Span.</summary>
    public static void ReverseByteOrder(Span<Int16> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = ReverseByteOrder(value);
      }
    }

    /// <summary>Reverses the byte order of all elements in the Span.</summary>
    [CLSCompliant(false)]
    public static void ReverseByteOrder(Span<Char> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = ReverseByteOrder(value);
      }
    }

    /// <summary>Reverses the byte order of all elements in the Span.</summary>
    [CLSCompliant(false)]
    public static void ReverseByteOrder(Span<UInt32> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = ReverseByteOrder(value);
      }
    }

    /// <summary>Reverses the byte order of all elements in the Span.</summary>
    public static void ReverseByteOrder(Span<Int32> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = ReverseByteOrder(value);
      }
    }

    /// <summary>Reverses the byte order of all elements in the Span.</summary>
    public static void ReverseByteOrder(Span<Single> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = ReverseByteOrder(value);
      }
    }

    /// <summary>Reverses the byte order of all elements in the Span.</summary>
    [CLSCompliant(false)]
    public static void ReverseByteOrder(Span<UInt64> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = ReverseByteOrder(value);
      }
    }

    /// <summary>Reverses the byte order of all elements in the Span.</summary>
    public static void ReverseByteOrder(Span<Int64> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = ReverseByteOrder(value);
      }
    }

    /// <summary>Reverses the byte order of all elements in the Span.</summary>
    public static void ReverseByteOrder(Span<Double> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = ReverseByteOrder(value);
      }
    }

    /// <summary>Returns copy where the individual Int32 components have a reversed byte order.
    /// Note: The order of the components itself is not reversed.</summary>
    public static void ReverseByteOrder(Span<Decimal> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = ReverseByteOrder(value);
      }
    }

    #endregion

    #endregion

    public static bool TryWriteBytes<T>(in T values, Span<byte> bytes, ref int index) where T : struct {
      int writeSize = Unsafe.SizeOf<T>();
      if ((bytes.Length - index) < writeSize)
        return false;
      ref var target = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(bytes), (IntPtr)index);
      Unsafe.WriteUnaligned(ref target, values);
      index += writeSize;
      return true;
    }

    public static bool TryWriteBytes<T>(ReadOnlySpan<T> values, Span<byte> bytes, ref int index) where T : struct {
      int writeSize = Unsafe.SizeOf<T>() * values.Length;
      if ((bytes.Length - index) < writeSize)
        return false;
      ref var target = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(bytes), (IntPtr)index);
      ref var source = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values));
      Unsafe.CopyBlockUnaligned(ref target, ref source, unchecked((uint)writeSize));
      index += writeSize;
      return true;
    }

    public static bool TryWriteBytes<T>(ReadOnlySpan<T> values, Span<byte> bytes, ref int index, Func<T, T> beforeWrite) where T : struct {
      int writeSize = Unsafe.SizeOf<T>() * values.Length;
      if ((bytes.Length - index) < writeSize)
        return false;
      ref var target = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(bytes), (IntPtr)index);
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        var current = beforeWrite(iterator.Current);
        Unsafe.WriteUnaligned(ref target, current);
      }
      index += writeSize;
      return true;
    }

    public static bool TryReadBytes<T>(ReadOnlySpan<byte> bytes, ref int index, ref T value) where T : struct {
      int readSize = Unsafe.SizeOf<T>();
      if ((bytes.Length - index) < readSize)
        return false;
      
      ref var source = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(bytes), (IntPtr)index);
      value = Unsafe.ReadUnaligned<T>(ref source);
      index += readSize;
      return true;
    }

    public static bool TryReadBytes<T>(ReadOnlySpan<byte> bytes, ref int index, Span<T> values) where T : struct {
      int readSize = Unsafe.SizeOf<T>() * values.Length;
      if ((bytes.Length - index) < readSize)
        return false;
      ref var target = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values));
      ref var source = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(bytes), (IntPtr)index);
      Unsafe.CopyBlockUnaligned(ref target, ref source, unchecked((uint)readSize));
      index += readSize;
      return true;
    }
  }
}
