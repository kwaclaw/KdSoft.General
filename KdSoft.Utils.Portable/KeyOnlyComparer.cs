using System.Collections.Generic;

namespace KdSoft.Utils
{
  /// <summary>
  /// This class can be used as comparer when a <see cref="SortedSet{T}"/> instance is to be used like a dictionary.
  /// It compares the key part of the items only.
  /// </summary>
  public class KeyOnlyComparer<TKey, TValue>: Comparer<KeyValuePair<TKey, TValue>>
  {
    internal IComparer<TKey> keyComparer;

    public KeyOnlyComparer() : this(null) { }

    public KeyOnlyComparer(IComparer<TKey>? keyComparer) {
      if (keyComparer == null) {
        this.keyComparer = Comparer<TKey>.Default;
      }
      else {
        this.keyComparer = keyComparer;
      }
    }

    public override int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) {
      return this.keyComparer.Compare(x.Key, y.Key);
    }
  }
}
