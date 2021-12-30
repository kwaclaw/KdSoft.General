using System;
using System.Collections.Generic;

namespace KdSoft.Utils
{
  /// <summary>
  /// Allows access to the wrapped value in ValueWrapper{T}.
  /// </summary>
  public interface IValueWrapper
  {
    object? Unwrap();
    Type GetValueType();
  }

  /// <summary>
  /// Wraps a value for the purpose of exposing it as a named property.
  /// This can be useful in serialization scenarios, where types but not properties can be annotated.
  /// </summary>
  /// <typeparam name="T">Type of wrapped value.</typeparam>
  public class ValueWrapper<T>: IValueWrapper
  {
    public ValueWrapper(T value) {
      this.Value = value;
    }

    public T Value { get; set; }

    public object? Unwrap() {
      return Value;
    }

    public Type GetValueType() {
      return typeof(T);
    }
  }

  /// <summary>
  /// Helper routines for ValueWrapper{T}.
  /// </summary>
  public static class ValueWrapper
  {
    public static ValueWrapper<T> Create<T>(T value) {
      return new ValueWrapper<T>(value);
    }

    public static IList<object?> Unwrap(this IList<IValueWrapper> wrappedvalues) {
      var result = new List<object?>(wrappedvalues.Count);
      for (int indx = 0; indx < wrappedvalues.Count; indx++)
        result.Add(wrappedvalues[indx].Unwrap());
      return result;
    }
  }
}
