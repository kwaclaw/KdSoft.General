using System;
using System.Collections.Generic;

namespace KdSoft.Utils
{
    /// <summary>
    /// <see cref="List{T}"/> extension methods. 
    /// </summary>
    public static class ListExtensions
    {
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
    }
}
