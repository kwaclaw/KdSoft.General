using System;
using System.Runtime.CompilerServices;

namespace KdSoft.Serialization
{
  /// <summary>
  /// Provides extension methods to manipulate value types.
  /// </summary>
  public static class ValueTypeExtensions
  {
    #region Reverse Byte Order

    /// <summary>Returns copy with reversed byte order.</summary>
    [CLSCompliant(false)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt16 ReverseByteOrder(this UInt16 value) {
      return ((ShortBytes)value).ReverseByteOrder();
    }

    /// <summary>Returns copy with reversed byte order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int16 ReverseByteOrder(this Int16 value) {
      return ((ShortBytes)value).ReverseByteOrder();
    }

    /// <summary>Returns copy with reversed byte order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Char ReverseByteOrder(this Char value) {
      return ((ShortBytes)value).ReverseByteOrder();
    }

    /// <summary>Returns copy with reversed byte order.</summary>
    [CLSCompliant(false)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt32 ReverseByteOrder(this UInt32 value) {
      return ((IntBytes)value).ReverseByteOrder();
    }

    /// <summary>Returns copy with reversed byte order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int32 ReverseByteOrder(this Int32 value) {
      return ((IntBytes)value).ReverseByteOrder();
    }

    /// <summary>Returns copy with reversed byte order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Single ReverseByteOrder(this Single value) {
      return ((IntBytes)value).ReverseByteOrder();
    }

    /// <summary>Returns copy with reversed byte order.</summary>
    [CLSCompliant(false)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt64 ReverseByteOrder(this UInt64 value) {
      return ((LongBytes)value).ReverseByteOrder();
    }

    /// <summary>Returns copy with reversed byte order.</summary>
    public static Int64 ReverseByteOrder(this Int64 value) {
      return ((LongBytes)value).ReverseByteOrder();
    }

    /// <summary>Returns copy with reversed byte order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double ReverseByteOrder(this Double value) {
      return ((LongBytes)value).ReverseByteOrder();
    }

    /// <summary>Returns copy where the individual Int32 components have a reversed byte order.
    /// Note: The order of the components itself is not reversed.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Decimal ReverseByteOrder(this Decimal value) {
      return ((DecimalBytes)value).ReverseByteOrder();
    }

    #endregion
  }
}
