using System;
using System.Collections.Generic;

namespace KdSoft.Utils
{
  /// <summary>
  /// Very basic ring queue of fixed size that relies on having elements added
  /// in sort order, so that binary search can be used.
  /// </summary>
  /// <remarks>Not thread-safe.</remarks>
  public class SortedRingQueue<T>
  {
    T[] items;
    IComparer<T> comparer;
    int head;
    int count;

    public SortedRingQueue(int size, IComparer<T> comparer) {
      items = new T[size];
      this.comparer = comparer;
    }

    public int Capacity {
      get { return items.Length; }
    }

    public int Count {
      get { return count; }
    }

    // does not check for index out of range
    int RealIndex(int index) {
      int result = head + index;
      if (result >= items.Length)
        result -= items.Length;
      return result;
    }

    public T this[int index] {
      get {
        if (count == 0)
          throw new InvalidOperationException("SortedRingQueue is empty.");
        if (index >= count || index < 0)
          throw new ArgumentOutOfRangeException();
        return items[RealIndex(index)];
      }
    }

    public T Head {
      get {
        if (count == 0)
          throw new InvalidOperationException("SortedRingQueue is empty.");
        return items[head];
      }
    }

    /// <summary>
    /// Tries to enqueue an item. Returns <c>false</c> when queue is full or item not in sort order.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="isSorted"></param>
    /// <returns></returns>
    public bool TryEnqueue(T item, out bool isSorted) {
      if (count == items.Length) {
        isSorted = true;
        return false;
      }
      if (count > 0) {
        int previous = head - 1;
        if (previous < 0)
          previous += items.Length;
        if (comparer.Compare(items[previous], item) < 0) {
          isSorted = false;
          return false;
        }
      }
      isSorted = true;
      items[head++] = item;
      if (head == items.Length)
        head = 0;
      count++;
      return true;
    }

    public bool TryPeek(out T? item) {
      if (count == 0) {
        item = default(T);
        return false;
      }
      int tail = head - count;
      if (tail < 0)
        tail += items.Length;
      item = items[tail];
      return true;
    }

    public bool TryDequeue(out T? item) {
      bool result = TryPeek(out item);
      if (result)
        count--;
      return result;
    }

    /// <summary>
    /// Finds an items if it exists, using binary search.
    /// </summary>
    /// <param name="item">Item to find.</param>
    /// <param name="index">Index of item found, or of insertion point - which is the index of the first item
    /// larger than the item given, or the index of the last item + 1, if all items are smaller.</param>
    /// <returns></returns>
    public bool BinaryFind(T item, out int index) {
      int start = 0;
      int end = count - 1;
      while (start <= end) {
        int middle = start + ((end - start) >> 1);
        int comp = comparer.Compare(items[RealIndex(middle)], item);
        if (comp == 0) {
          index = middle;
          return true;
        }
        if (comp < 0) {
          start = middle + 1;
        }
        else {
          end = middle - 1;
        }
      }
      index = start;
      return false;
    }
  }
}
