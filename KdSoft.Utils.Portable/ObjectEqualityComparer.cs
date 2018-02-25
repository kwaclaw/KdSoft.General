using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KdSoft.Utils.Portable
{
  /// <summary>
  /// Implementation of <see cref="IEqualityComparer{T}"/> that compares objects
  /// by reference and calculates their hash code based on the reference only.
  /// This is useful when objects need to be tracked based on their object handle
  /// even if <see cref="Object.Equals(object)"/> and <see cref="Object.GetHashCode"/>
  /// have been overridden to implement value type semantics. 
  /// </summary>
  public class ObjectEqualityComparer : IEqualityComparer<object>
  {
    /// <inheritdoc />
    public new bool Equals(object x, object y) {
      return object.ReferenceEquals(x, y);
    }

    /// <inheritdoc />
    public int GetHashCode(object obj) {
      return RuntimeHelpers.GetHashCode(obj);
    }
  }

}
