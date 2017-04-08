using System;
using System.Collections.Generic;

namespace KdSoft.Utils
{
  /// <summary>
  /// <see cref="List{T}"/> extension methods. 
  /// </summary>
  public static class ListExtensions
  {
    #region ToSortedList

    /// <summary>
    /// Creates a sorted <see cref="List{T}"/> from an <see cref="IEnumerable{T}"/> using the default comparer.
    /// </summary>
    /// <typeparam name="T">Type of list items.</typeparam>
    /// <param name="collection"><see cref="IEnumerable{T}"/> to create a list from.</param>
    /// <returns>Sorted <see cref="List{T}"/>.</returns>
    public static List<T> ToSortedList<T>(this IEnumerable<T> collection) {
      var result = new List<T>(collection);
      result.Sort();
      return result;
    }

    /// <summary>
    /// Creates a sorted <see cref="List{T}"/> from an <see cref="IEnumerable{T}"/> using a specified 
    /// <see cref="Comparison{T}"/> comparer.
    /// </summary>
    /// <typeparam name="T">Type of list items.</typeparam>
    /// <param name="collection"><see cref="IEnumerable{T}"/> to create a list from.</param>
    /// <returns>Sorted <see cref="List{T}"/>.</returns>
    public static List<T> ToSortedList<T>(this IEnumerable<T> collection, Comparison<T> comparer) {
      var result = new List<T>(collection);
      result.Sort(comparer);
      return result;
    }

    /// <summary>
    /// Creates a sorted <see cref="List{T}"/> from an <see cref="IEnumerable{T}"/> using a specified 
    /// <see cref="IComparer{T}"/> comparer.
    /// </summary>
    /// <typeparam name="T">Type of list items.</typeparam>
    /// <param name="collection"><see cref="IEnumerable{T}"/> to create a list from.</param>
    /// <returns>Sorted <see cref="List{T}"/>.</returns>
    public static List<T> ToSortedList<T>(this IEnumerable<T> collection, IComparer<T> comparer) {
      var result = new List<T>(collection);
      result.Sort(comparer);
      return result;
    }

    #endregion

    #region DistinctWhenSorted

    /// <summary>
    /// Removes duplicate items from a sorted list.
    /// Which duplicate item get removed is not deterministic.
    /// </summary>
    /// <typeparam name="T">Type of list items.</typeparam>
    /// <param name="list">Sorted <see cref="List{T}"/> to makie distinct.</param>
    /// <param name="equals">Delegate to be used for list item equality comparison.
    /// Must be compatible with sorting comparison logic.</param>
    public static void DistinctWhenSorted<T>(this List<T> list, Func<T, T, bool> equals = null) {
      if (list.Count == 0)
        return;
      if (equals == null)
        equals = EqualityComparer<T>.Default.Equals;

      int indx = 1;
      int distinctIndx = 0;
      var distinctItem = list[0];

      for (; indx < list.Count; indx++) {
        var nextItem = list[indx];
        if (equals(list[indx], distinctItem)) {
          // skip
        }
        else {
          distinctIndx++;
          if (indx > distinctIndx)
            list[distinctIndx] = nextItem;
          distinctItem = nextItem;
        }
      }

      int distinctCount = distinctIndx + 1;
      list.RemoveRange(distinctCount, list.Count - distinctCount);
    }

    /// <summary>
    /// Delegate to be used for picking one of two arguments.
    /// </summary>
    /// <typeparam name="T">Type of arguments.</typeparam>
    /// <param name="first">First argument.</param>
    /// <param name="second">Second argument.</param>
    /// <returns><c>true</c> to pick the first argument, <c>false</c> to pick the second argument.</returns>
    public delegate bool PickFirst<T>(T first, T second);

    /// <summary>
    /// Removes duplicate items from a sorted list.
    /// </summary>
    /// <typeparam name="T">Type of list items.</typeparam>
    /// <param name="list">Sorted <see cref="List{T}"/> to makie distinct.</param>
    /// <param name="keepNext">Delegate that is used to determine which duplicate items get removed.
    /// The first item argument is the item following the "original/first" duplicate. Return true to keep
    /// the first item (the "next duplicate" in the list) and remove the previous one.</param>
    /// <param name="equals">Delegate to be used for list item equality comparison.
    /// Must be compatible with sorting comparison logic.</param>
    public static void DistinctWhenSorted<T>(this List<T> list, PickFirst<T> keepNext, Func<T, T, bool> equals = null) {
      if (list.Count == 0)
        return;
      if (equals == null)
        equals = EqualityComparer<T>.Default.Equals;

      int indx = 1;
      int distinctIndx = 0;
      var distinctItem = list[0];

      for (; indx < list.Count; indx++) {
        var nextItem = list[indx];
        if (equals(nextItem, distinctItem)) {
          if (keepNext(nextItem, distinctItem)) {
            list[distinctIndx] = nextItem;
          }
        }
        else {
          distinctIndx++;
          if (indx > distinctIndx)
            list[distinctIndx] = nextItem;
          distinctItem = nextItem;
        }
      }

      int distinctCount = distinctIndx + 1;
      list.RemoveRange(distinctCount, list.Count - distinctCount);
    }

    #endregion

    #region BinarySearch

    /// <summary>
    /// Performs a binary range search on a sorted <see cref="IList{T}"/> using the
    /// specified compare delegate and returns the zero-based index of the matching element.
    /// </summary>
    /// <typeparam name="T">Type of list elements.</typeparam>
    /// <param name="list">The <see cref="IList{T}"/> instance to search.</param>
    /// <param name="index">Starting index of search range.</param>
    /// <param name="length">Number of elements in search range.</param>
    /// <param name="compare">Delegate that compares each list item to a value
    /// contained within the delegate itself. Such a delegate can be constructed from a
    /// <see cref="Comparison{T}"/> like this: <c>(x) => comparison(x, value)</c>.</param>
    /// <returns>
    /// The zero-based index of the matching element in the sorted System.Collections.Generic.List`1,
    /// if the comparison returns <c>0</c>; otherwise, a negative number that is the bitwise complement
    /// of the index of the next element that compares as larger or, if there is no
    /// larger element, the bitwise complement of System.Collections.Generic.List`1.Count.
    /// </returns>
    static int BinarySearchImpl<T>(IList<T> list, int index, int length, Func<T, int> compare) {
      int lo = index;
      int hi = index + length - 1;
      while (lo <= hi) {
        int i = lo + ((hi - lo) >> 1);
        int order = compare(list[i]);

        if (order == 0) return i;
        if (order < 0) {
          lo = i + 1;
        }
        else {
          hi = i - 1;
        }
      }

      return ~lo;
    }

    /// <summary>
    /// Performs a binary range search on a sorted <see cref="IList{T}"/> using the
    /// specified compare delegate and returns the zero-based index of the matching element.
    /// </summary>
    /// <typeparam name="T">Type of list elements.</typeparam>
    /// <param name="list">The <see cref="IList{T}"/> instance to search.</param>
    /// <param name="index">Starting index of search range.</param>
    /// <param name="length">Number of elements in search range.</param>
    /// <param name="compare">Delegate that compares each list item to a value
    /// contained within the delegate itself. Such a delegate can be constructed from a
    /// <see cref="Comparison{T}"/> like this: <c>(x) => comparison(x, value)</c>.</param>
    /// <returns>
    /// The zero-based index of the matching element in the sorted System.Collections.Generic.List`1,
    /// if the comparison returns <c>0</c>; otherwise, a negative number that is the bitwise complement
    /// of the index of the next element that compares as larger or, if there is no
    /// larger element, the bitwise complement of System.Collections.Generic.List`1.Count.
    /// </returns>
    public static int BinarySearch<T>(this IList<T> list, int index, int length, Func<T, int> compare) {
      if (compare == null)
        throw new ArgumentNullException(nameof(compare));
      if (index >= list.Count || index + length > list.Count)
        throw new IndexOutOfRangeException();
      return BinarySearchImpl(list, index, length, compare);
    }


    /// <summary>
    /// Performs a binary search on an entire sorted <see cref="IList{T}"/> using the
    /// specified compare delegate and returns the zero-based index of the matching element.
    /// </summary>
    /// <typeparam name="T">Type of list elements.</typeparam>
    /// <param name="list">The <see cref="IList{T}"/> instance to search.</param>
    /// <param name="compare">Delegate that compares each list item to a value
    /// contained within the delegate itself. Such a delegate can be constructed from a
    /// <see cref="Comparison{T}"/> like this: <c>(x) => comparison(x, value)</c>.</param>
    /// <returns>
    /// The zero-based index of the matching element in the sorted System.Collections.Generic.List`1,
    /// if the comparison returns <c>0</c>; otherwise, a negative number that is the bitwise complement
    /// of the index of the next element that compares as larger or, if there is no
    /// larger element, the bitwise complement of System.Collections.Generic.List`1.Count.
    /// </returns>
    public static int BinarySearch<T>(this IList<T> list, Func<T, int> compare) {
      if (compare == null)
        throw new ArgumentNullException(nameof(compare));
      return BinarySearchImpl(list, 0, list.Count, compare);
    }

    /// <summary>
    /// Performs a binary range search for a given element on a sorted <see cref="IList{T}"/>
    /// using the specified compare delegate and returns the zero-based index of the element.
    /// </summary>
    /// <typeparam name="T">Type of list elements.</typeparam>
    /// <param name="list">The <see cref="IList{T}"/> instance to search.</param>
    /// <param name="index">Starting index of search range.</param>
    /// <param name="length">Number of elements in search range.</param>
    /// <param name="item">Item to find, i.e. to compare list elements against.</param>
    /// <param name="compare">Delegate that compares each list item to the given element.</param>
    /// <returns>
    /// The zero-based index of the element in the sorted System.Collections.Generic.List`1,
    /// if the element is found; otherwise, a negative number that is the bitwise complement
    /// of the index of the next element that is larger than the given element or, if there is no
    /// larger element, the bitwise complement of System.Collections.Generic.List`1.Count.
    /// </returns>
    public static int BinarySearch<T>(this IList<T> list, int index, int length, T item, Comparison<T> compare = null) {
      if (index >= list.Count || index + length > list.Count)
        throw new IndexOutOfRangeException();
      if (compare == null)
        compare = Comparer<T>.Default.Compare;
      return BinarySearchImpl(list, index, length, (x) => compare(x, item));
    }

    /// <summary>
    /// Performs a binary search for a given element on an entire sorted <see cref="IList{T}"/>
    /// using the specified compare delegate and returns the zero-based index of the element.
    /// </summary>
    /// <typeparam name="T">Type of list elements.</typeparam>
    /// <param name="list">The <see cref="IList{T}"/> instance to search.</param>
    /// <param name="item">Item to find, i.e. to compare list elements against.</param>
    /// <param name="compare">Delegate that compares each list item to the given element.</param>
    /// <returns>
    /// The zero-based index of the element in the sorted System.Collections.Generic.List`1,
    /// if the element is found; otherwise, a negative number that is the bitwise complement
    /// of the index of the next element that is larger than the given element or, if there is no
    /// larger element, the bitwise complement of System.Collections.Generic.List`1.Count.
    /// </returns>
    public static int BinarySearch<T>(this IList<T> list, T item, Comparison<T> compare = null) {
      if (compare == null)
        compare = Comparer<T>.Default.Compare;
      return BinarySearchImpl(list, 0, list.Count, (x) => compare(x, item));
    }

    /// <summary>
    /// Performs a binary range search for a given element on a sorted <see cref="IList{T}"/>
    /// using the specified <see cref="IComparer{T}"/> and returns the zero-based index of the element.
    /// </summary>
    /// <typeparam name="T">Type of list elements.</typeparam>
    /// <param name="list">The <see cref="IList{T}"/> instance to search.</param>
    /// <param name="index">Starting index of search range.</param>
    /// <param name="length">Number of elements in search range.</param>
    /// <param name="item">Item to find, i.e. to compare list elements against.</param>
    /// <param name="comparer"><see cref="IComparer{T}"/> to use for comparing list items to the given element.</param>
    /// <returns>
    /// The zero-based index of the element in the sorted System.Collections.Generic.List`1,
    /// if the element is found; otherwise, a negative number that is the bitwise complement
    /// of the index of the next element that is larger than the given element or, if there is no
    /// larger element, the bitwise complement of System.Collections.Generic.List`1.Count.
    /// </returns>
    public static int BinarySearch<T>(this IList<T> list, int index, int length, T item, IComparer<T> comparer) {
      if (index >= list.Count || index + length > list.Count)
        throw new IndexOutOfRangeException();
      if (comparer == null)
        comparer = Comparer<T>.Default;
      return BinarySearchImpl(list, index, length, (x) => comparer.Compare(x, item));
    }

    #endregion

    #region FindIndex

    static int FindIndexImpl<T>(IList<T> source, int startIndex, int endIndex, Predicate<T> match) {
      for (int i = startIndex; i < endIndex; i++) {
        if (match(source[i])) {
          return i;
        }
      }
      return -1;
    }

    /// <summary>
    /// Searches an <see cref="IList{T}"/> for an element that matches the conditions defined
    /// by the specified predicate, and returns the zero-based index of the first occurrence
    /// within the specified search range.
    /// </summary>
    /// <typeparam name="T">Type of list items.</typeparam>
    /// <param name="source"><see cref="IList{T}"/> to search.</param>
    /// <param name="startIndex">Index to start searching at.</param>
    /// <param name="count">Number of items to check following the startIndex.</param>
    /// <param name="match">The Predicate{T} to use for matching the list items.</param>
    /// <returns></returns>
    public static int FindIndex<T>(this List<T> source, int startIndex, int count, Predicate<T> match) {
      return FindIndexImpl(source, startIndex, startIndex + count, match);
    }

    /// <summary>
    /// Searches an <see cref="IList{T}"/> for an element that matches the conditions defined
    /// by the specified predicate, and returns the zero-based index of the first occurrence
    /// after the specified start index.
    /// </summary>
    /// <typeparam name="T">Type of list items.</typeparam>
    /// <param name="source"><see cref="IList{T}"/> to search.</param>
    /// <param name="startIndex">Index to start searching at.</param>
    /// <param name="match">The Predicate{T} to use for matching the list items.</param>
    /// <returns></returns>
    public static int FindIndex<T>(this IList<T> source, int startIndex, Predicate<T> match) {
      return FindIndexImpl(source, startIndex, source.Count, match);
    }

    /// <summary>
    /// Searches an <see cref="IList{T}"/> for an element that matches the conditions defined
    /// by the specified predicate, and returns the zero-based index of the first occurrence.
    /// </summary>
    /// <typeparam name="T">Type of list items.</typeparam>
    /// <param name="source"><see cref="IList{T}"/> to search.</param>
    /// <param name="match">The Predicate{T} to use for matching the list items.</param>
    /// <returns></returns>
    public static int FindIndex<T>(this IList<T> source, Predicate<T> match) {
      return FindIndexImpl(source, 0, source.Count, match);
    }

    #endregion
  }
}
