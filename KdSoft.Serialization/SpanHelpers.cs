using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KdSoft.Serialization.Buffer
{
  /// <summary>
  /// Provides helper methods to manipulate, read and write <see cref="Span{T}"/> where <c>T</c> is a value type.
  /// </summary>
  public static class SpanHelpers
  {
    #region Reverse Byte Order

    /// <summary>Reverses the byte order of all elements in the Span in place.</summary>
    [CLSCompliant(false)]
    public static void ReverseByteOrder(Span<UInt16> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = value.ReverseByteOrder();
      }
    }

    /// <summary>Reverses the byte order of all elements in the Span in place.</summary>
    public static void ReverseByteOrder(Span<Int16> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = value.ReverseByteOrder();
      }
    }

    /// <summary>Reverses the byte order of all elements in the Span in place.</summary>
    [CLSCompliant(false)]
    public static void ReverseByteOrder(Span<Char> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = value.ReverseByteOrder();
      }
    }

    /// <summary>Reverses the byte order of all elements in the Span in place.</summary>
    [CLSCompliant(false)]
    public static void ReverseByteOrder(Span<UInt32> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = value.ReverseByteOrder();
      }
    }

    /// <summary>Reverses the byte order of all elements in the Span in place.</summary>
    public static void ReverseByteOrder(Span<Int32> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = value.ReverseByteOrder();
      }
    }

    /// <summary>Reverses the byte order of all elements in the Span in place.</summary>
    public static void ReverseByteOrder(Span<Single> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = value.ReverseByteOrder();
      }
    }

    /// <summary>Reverses the byte order of all elements in the Span in place.</summary>
    [CLSCompliant(false)]
    public static void ReverseByteOrder(Span<UInt64> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = value.ReverseByteOrder();
      }
    }

    /// <summary>Reverses the byte order of all elements in the Span in place.</summary>
    public static void ReverseByteOrder(Span<Int64> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = value.ReverseByteOrder();
      }
    }

    /// <summary>Reverses the byte order of all elements in the Span in place.</summary>
    public static void ReverseByteOrder(Span<Double> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = value.ReverseByteOrder();
      }
    }

    /// <summary>Reverses the byte order of all elements in the Span in place.
    /// Note: Refer to <see cref="ValueTypeExtensions.ReverseByteOrder(decimal)"/> for specific details on <see cref="decimal"/>.
    /// </summary>
    public static void ReverseByteOrder(Span<Decimal> values) {
      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        ref var value = ref iterator.Current;
        value = value.ReverseByteOrder();
      }
    }

    #endregion

    #region Read and Write value types to and from Span<byte>

    //TODO For TryWriteBytes (but not the reverse version) use BitConverter.TryWriteBytes when it becomes available?

    /// <summary>
    /// Write value type to <see cref="Span{T}"/> of bytes.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="value">Value to serialize.</param>
    /// <param name="bytes"><see cref="Span{T}"/> of bytes to write to.</param>
    /// <param name="index">Index in <c>Span&lt;byte></c> to start writing at.
    ///   Will be updated to return the position after the last written byte.</param>
    /// <returns><c>true</c> if value could be written successfully (sufficient space), <c>false</c> otherwise.</returns>
    public static bool TryWriteBytes<T>(in T value, Span<byte> bytes, ref int index) where T : struct {
      int writeSize = Unsafe.SizeOf<T>();
      if ((bytes.Length - index) < writeSize)
        return false;

      ref var target = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(bytes), (IntPtr)index);
      Unsafe.WriteUnaligned(ref target, value);

      index += writeSize;
      return true;
    }

    /// <summary>
    /// Write <see cref="ReadOnlySpan{T}"/> of value type to <see cref="Span{T}"/> of bytes.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="values"><see cref="ReadOnlySpan{T}"/> of value type to serialize.</param>
    /// <param name="bytes"><see cref="Span{T}"/> of bytes to write to.</param>
    /// <param name="index">Index in <c>Span&lt;byte></c> to start writing at.
    ///   Will be updated to return the position after the last written byte.</param>
    /// <returns><c>true</c> if values could be written successfully (sufficient space), <c>false</c> otherwise.</returns>
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

    /// <summary>
    /// Write <see cref="ReadOnlySpan{T}"/> of value type to <see cref="Span{T}"/> of bytes.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="values"><see cref="ReadOnlySpan{T}"/> of value type to serialize.</param>
    /// <param name="bytes"><see cref="Span{T}"/> of bytes to write to.</param>
    /// <param name="index">Index in <c>Span&lt;byte></c> to start writing at.
    ///   Will be updated to return the position after the last written byte.</param>
    /// <param name="beforeWrite">Callback that gets executed for each item in the input, just before
    ///   it is written to the <c>Span&lt;byte></c>. This allows for conversions like byte re-ordering.</param>
    /// <returns><c>true</c> if values could be written successfully (sufficient space), <c>false</c> otherwise.</returns>
    public static bool TryWriteBytes<T>(ReadOnlySpan<T> values, Span<byte> bytes, ref int index, Func<T, T> beforeWrite) where T : struct {
      int itemSize = Unsafe.SizeOf<T>();
      int writeSize = itemSize * values.Length;
      if ((bytes.Length - index) < writeSize)
        return false;

      ref byte bytesRef = ref MemoryMarshal.GetReference(bytes);
      IntPtr writeIndex = (IntPtr)index;

      var iterator = values.GetEnumerator();
      while (iterator.MoveNext()) {
        var current = beforeWrite(iterator.Current);
        ref byte target = ref Unsafe.AddByteOffset(ref bytesRef, writeIndex);
        Unsafe.WriteUnaligned(ref target, current);
        writeIndex += itemSize;
      }

      index = (int)writeIndex;
      return true;
    }

    /// <summary>
    /// Read value type from <see cref="ReadOnlySpan{T}"/> of bytes.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="bytes"><see cref="ReadOnlySpan{T}"/> of bytes to read from.</param>
    /// <param name="index">Index in <c>ReadOnlySpan&lt;byte></c> to start reading from.
    ///   Will be updated to return the position after the last read byte.</param>
    /// <param name="value">Value to deserialize.</param>
    /// <returns><c>true</c> if value could be read successfully (sufficient space), <c>false</c> otherwise.</returns>
    public static bool TryReadBytes<T>(ReadOnlySpan<byte> bytes, ref int index, ref T value) where T : struct {
      int readSize = Unsafe.SizeOf<T>();
      if ((bytes.Length - index) < readSize)
        return false;

      ref var source = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(bytes), (IntPtr)index);
      value = Unsafe.ReadUnaligned<T>(ref source);

      index += readSize;
      return true;
    }

    /// <summary>
    /// Read <see cref="Span{T}"/> of value type from <see cref="ReadOnlySpan{T}"/> of bytes.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="bytes"><see cref="ReadOnlySpan{T}"/> of bytes to read from.</param>
    /// <param name="index">Index in <c>ReadOnlySpan&lt;byte></c> to start reading from.
    ///   Will be updated to return the position after the last read byte.</param>
    /// <param name="values"><see cref="Span{T}"/> of value type to deserialize.</param>
    /// <returns><c>true</c> if values could be read successfully (sufficient space), <c>false</c> otherwise.</returns>
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

    #endregion
  }
}
