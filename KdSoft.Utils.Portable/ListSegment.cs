#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using System;
using System.Collections;
using System.Collections.Generic;

namespace KdSoft.Utils
{
  /// <summary>
  /// This is a read-only <see cref="IList{T}"/> implementation representing a segment of a list. Similar to <see cref="ArraySegment{T}"/>.
  /// </summary>
  public class ListSegment<T>: IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable
  {
    IList<T> list;
    int offset;
    int count;

    public ListSegment() : this(new T[0]) { }

    public ListSegment(IList<T> list) : this(list, 0, list.Count) { }

    public ListSegment(IList<T> list, int offset, int count) {
      Initialize(list, offset, count);
    }

    public ListSegment<T> Initialize(IList<T> list, int offset, int count) {
      this.list = list;
      this.offset = offset;
      this.count = count;
      return this;
    }

    public T this[int index] {
      get {
        if (index >= count)
          throw new IndexOutOfRangeException();
        return list[offset + index];
      }
      set {
        throw new NotSupportedException();
      }
    }

    public int Count {
      get { return count; }
    }

    public ListSegment<T> GetSegment(int offset, int count) {
      if (offset >= this.count || offset + count > this.count)
        throw new IndexOutOfRangeException();
      return new ListSegment<T>(list, this.offset + offset, count);
    }

    public bool Contains(T item) {
      return IndexOf(item) >= 0;
    }

    public bool Exists(Predicate<T> match) {
      return FindIndex(match) != -1;
    }

    public int FindIndex(Predicate<T> match) {
      int limit = offset + count;
      for (int i = offset; i < limit; ++i)
        if (match(list[i]))
          return i - offset;
      return -1;
    }

    public int FindIndex(int startIndex, Predicate<T> match) {
      if (startIndex >= count)
        throw new IndexOutOfRangeException();

      int limit = offset + count;
      for (int i = offset + startIndex; i < limit; ++i)
        if (match(list[i]))
          return i - offset;
      return -1;
    }

    public int FindIndex(int startIndex, int count, Predicate<T> match) {
      if (startIndex >= this.count || startIndex + count > this.count)
        throw new IndexOutOfRangeException();

      int limit = offset + startIndex + count;
      for (int i = offset + startIndex; i < limit; ++i)
        if (match(list[i]))
          return i - offset;
      return -1;
    }

    public int FindLastIndex(Predicate<T> match) {
      for (int i = offset + count; --i >= offset; ++i)
        if (match(list[i]))
          return i - offset;
      return -1;
    }

    public int FindLastIndex(int startIndex, Predicate<T> match) {
      if (startIndex >= count)
        throw new IndexOutOfRangeException();

      int limit = offset + startIndex;
      for (int i = offset + count; --i >= limit; ++i)
        if (match(list[i]))
          return i - offset;
      return -1;
    }

    public int FindLastIndex(int startIndex, int count, Predicate<T> match) {
      if (startIndex >= this.count || startIndex + count > this.count)
        throw new IndexOutOfRangeException();

      int limit = offset + startIndex;
      for (int i = offset + count; --i >= limit; ++i)
        if (match(list[i]))
          return i - offset;
      return -1;
    }

    public T? Find(Predicate<T> match) {
      int limit = offset + count;
      for (int i = offset; i < limit; ++i)
        if (match(list[i]))
          return list[i];
      return default(T);
    }

    public T? FindLast(Predicate<T> match) {
      for (int i = offset + count; --i >= offset;)
        if (match(list[i]))
          return list[i];
      return default(T);
    }

    public void ForEach(Action<T> action) {
      for (int i = offset; i < count; ++i)
        action(list[i]);
    }

    public bool TrueForAll(Predicate<T> match) {
      for (int i = offset; i < count; ++i)
        if (!match(list[i]))
          return false;
      return true;
    }

    public List<T> FindAll(Predicate<T> match) {
      List<T> result = new List<T>();
      int limit = offset + count;
      for (int i = offset; i < limit; ++i)
        if (match(list[i]))
          result.Add(list[i]);
      return result;
    }

    public int IndexOf(T item) {
      int limit = offset + count;
      for (int i = offset; i < limit; ++i)
        if (object.Equals(list[i], item))
          return i - offset;
      return -1;
    }

    public void Insert(int index, T item) {
      throw new NotSupportedException();
    }

    public void RemoveAt(int index) {
      throw new NotSupportedException();
    }

    public void Add(T item) {
      throw new NotSupportedException();
    }

    public void Clear() {
      throw new NotSupportedException();
    }

    public void CopyTo(T[] array, int arrayIndex) {
      int limit = offset + count;
      for (int i = offset; i < limit; ++i)
        array[arrayIndex++] = list[i];
    }

    public bool IsReadOnly {
      get { return true; }
    }

    public bool Remove(T item) {
      throw new NotSupportedException();
    }

    public IEnumerator<T> GetEnumerator() {
      int limit = offset + count;
      for (int i = offset; i < limit; ++i)
        yield return list[i];
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }
  }
}
