using System;
using System.Collections;
using System.Collections.Generic;

namespace KdSoft.Utils
{
    /// <summary>
    /// Exposes a <see cref="Comparison{T}"/> through an <see cref="IComparer{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">Type to compare.</typeparam>
    /// <remarks>To prevent boxing the struct when used as an interface, use this trick if possible:
    /// <code>
    /// class Util
    /// {
    ///   public bool Compare&lt;C, T&gt;(C comparer, T val1, T val2) where C: IComparer&lt;T&gt; {
    ///     return comparer(val1, val2);
    ///   }
    /// }
    /// ...
    /// var myComparer = new LambdaComparer&lt;MyClass&gt;(myDelegate);
    /// var t1 = new MyClass(...); var t2 = new MyClass(...);
    /// bool result = utilInstance.Compare(myComparer, t1, t2);
    /// </code>
    /// </remarks>
    public struct LambdaComparer<T>: IComparer<T>, IComparer
    {
        Comparison<T> compare;

        public LambdaComparer(Comparison<T>? compare) {
            if (compare == null)
                throw new ArgumentNullException("compare");
            this.compare = compare;
        }

        public int Compare(T? x, T? y) {
            if (object.ReferenceEquals(x, null))
                return object.ReferenceEquals(y, null) ? 0 : -1;
            else if (object.ReferenceEquals(y, null))
                return 1;
            return compare(x, y);
        }

        public int Compare(object? x, object? y) {
            if (object.ReferenceEquals(x, null))
                return object.ReferenceEquals(y, null) ? 0 : -1;
            else if (object.ReferenceEquals(y, null))
                return 1;
            if (x is T x1 && y is T y1)
                return compare(x1, y1);
            throw new ArgumentException("Cannot compare objects of type " + x.GetType().Name + " and " + y.GetType().Name);
        }
    }
}