using System;
using System.Collections.Generic;
using System.Linq;

namespace KdSoft.Utils
{
    public static class LinqExtensions
    {
        /// <summary>
        /// Merges two collections that are sorted by the same key criteria and produces a sorted collection.
        /// The collections must be sets, that is, they must not have duplicates according to the sort criteria.
        /// </summary>
        /// <typeparam name="TOuter">Element type of outer collection.</typeparam>
        /// <typeparam name="TInner">Element type of inner collection.</typeparam>
        /// <typeparam name="TResult">Result type.</typeparam>
        /// <param name="outer">Outer or "left" collection.</param>
        /// <param name="inner">Inner or "right" collection.</param>
        /// <param name="comparer">Delegate to compare two collection elements.</param>
        /// <param name="outerSelector">
        ///     Result selector to apply when only an outer element is available (no matching inner element).
        ///     If <c>null</c>, then no unmatched outer elements will be returned.
        /// </param>
        /// <param name="innerSelector">
        ///     Result selector to apply when only an inner element is available (no matching outer element).
        ///     If <c>null</c>, then no unmatched inner elements will be returned.
        /// </param>
        /// <param name="matchSelector">
        ///     Result selector to apply when matching outer and inner elements are available.
        ///     If <c>null</c>, then no matched outer/outer elements will be returned.
        /// </param>
        /// <returns>Sorted collection of result type elements.</returns>
        /// <remarks>
        ///     Depending on which selectors are <c>null</c>, certain types of set operations or joins can be achieved:
        ///     <list type="bullet">
        ///         <item>All selectors are <c>!= null</c>: Set Union, returns result elements
        ///             for all elements from both sets, in sorted order. This can also be considered 
        ///             a full outer join (with the restriction that the sort has no duplicates).</item>
        ///         <item><c>outerSelector != null (other selectors are null)</c>: Set Difference, returns result
        ///             elements for all those elements in outer that do not have a matching an element in inner.</item>
        ///         <item><c>outerSelector != null &amp;&amp; matchSelector != null</c>: Left Outer Join, returns
        ///             result elements for all elements in outer, but only for matching elements in inner.</item>
        ///         <item><c>innerSelector != null (other selectors are null)</c>: Set difference, returns
        ///             result elements for all those elements in inner that do not have a matching element in outer.</item>
        ///         <item><c>innerSelector != null &amp;&amp; matchSelector != null</c>: Right Outer Join, returns
        ///             result elements for all elements in inner, but only for matching elements in outer.</item>
        ///         <item><c>matchSelector != null (other selectors are null)</c>: Set Intersection,
        ///             returns result elements only for matching elements in outer and inner.
        ///             This can also be considered an inner join (but without duplicates).</item>
        ///         <item><c>matchSelector == null (other selectors are not null)</c>: Symmetric Difference,
        ///             opposite of intersection, returns result elements only for unmatched elements.</item>
        ///     </list>
        /// </remarks>
        public static IEnumerable<TResult> SortedMerge<TOuter, TInner, TResult>(
        this IEnumerable<TOuter> outer,
        IEnumerable<TInner> inner,
        Func<TOuter, TInner, int> comparer,
        Func<TOuter, TResult> outerSelector,
        Func<TInner, TResult> innerSelector,
        Func<TOuter, TInner, TResult> matchSelector) {
            if (outer == null)
                throw new ArgumentNullException("outer");
            if (inner == null)
                throw new ArgumentNullException("inner");
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            var outerator = outer.GetEnumerator();
            var innerator = inner.GetEnumerator();

            bool hasOuter = outerator.MoveNext();
            bool hasInner = innerator.MoveNext();

            while (true) {
                if (hasOuter && hasInner) {
                    var outerItem = outerator.Current;
                    var innerItem = innerator.Current;
                    int compValue = comparer(outerItem, innerItem);
                    if (compValue < 0) {
                        if (outerSelector != null)
                            yield return outerSelector(outerItem);
                        hasOuter = outerator.MoveNext();
                    }
                    else if (compValue > 0) {
                        if (innerSelector != null)
                            yield return innerSelector(innerItem);
                        hasInner = innerator.MoveNext();
                    }
                    else {
                        if (matchSelector != null)
                            yield return matchSelector(outerItem, innerItem);
                        hasOuter = outerator.MoveNext();
                        hasInner = innerator.MoveNext();
                    }
                }
                else if (hasOuter) {
                    if (outerSelector == null)  // we are done
                        break;
                    yield return outerSelector(outerator.Current);
                    hasOuter = outerator.MoveNext();
                }
                else if (hasInner) {
                    if (innerSelector == null)  // we are done
                        break;
                    yield return innerSelector(innerator.Current);
                    hasInner = innerator.MoveNext();
                }
                else
                    break;
            }
        }

        /// <summary>
        /// Like <see cref="Enumerable.GroupBy"/>, but takes advantage of an existing sort order on the collection.
        /// </summary>
        /// <typeparam name="TSource">Element type of source collection to be grouped.</typeparam>
        /// <typeparam name="TKey">Type of key that the collection is sorted on.</typeparam>
        /// <typeparam name="TElement">Type of element to return.</typeparam>
        /// <param name="source">Source collection.</param>
        /// <param name="keySelector">Extracts key from source element.</param>
        /// <param name="elementSelector">Extract result element from source element.</param>
        /// <param name="comparer">Equality comparer for the keys.</param>
        /// <returns>Collection of groups.</returns>
        public static IEnumerable<IGrouping<TKey, TElement>> SortedGroupBy<TSource, TKey, TElement>(
          this IEnumerable<TSource> source,
          Func<TSource, TKey> keySelector,
          Func<TSource, TElement> elementSelector,
          IEqualityComparer<TKey> comparer = null) {
            if (source == null)
                throw new ArgumentNullException("source");
            var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
                yield break;
            if (keySelector == null)
                throw new ArgumentNullException("keySelector");
            if (elementSelector == null)
                throw new ArgumentNullException("elementSelector");
            if (comparer == null)
                comparer = EqualityComparer<TKey>.Default;

            var group = new List<TElement>();
            group.Add(elementSelector(enumerator.Current));
            var key = keySelector(enumerator.Current);

            while (enumerator.MoveNext()) {
                var currentKey = keySelector(enumerator.Current);
                var currentElement = elementSelector(enumerator.Current);
                if (comparer.Equals(key, currentKey))
                    group.Add(currentElement);
                else {
                    var grouping = new Grouping<TKey, TElement>(key, group.ToArray());
                    group.Clear();
                    group.Add(currentElement);
                    key = currentKey;
                    yield return grouping;
                }
            }

            if (group.Count > 0) {
                group.TrimExcess();
                yield return new Grouping<TKey, TElement>(key, group);
            }
        }

        /// <summary>
        /// Like <see cref="Enumerable.GroupBy"/>, but takes advantage of an existing sort order on the collection.
        /// </summary>
        /// <typeparam name="TElement">Element type of collection to be grouped.</typeparam>
        /// <typeparam name="TKey">Type of key that the collection is sorted on.</typeparam>
        /// <param name="elements">Collection to be grouped.</param>
        /// <param name="keySelector">Extracts key from element.</param>
        /// <param name="comparer">Equality comparer for the keys.</param>
        /// <returns>Collection of groups.</returns>
        public static IEnumerable<IGrouping<TKey, TElement>> SortedGroupBy<TElement, TKey>(
          this IEnumerable<TElement> elements,
          Func<TElement, TKey> keySelector,
          IEqualityComparer<TKey> comparer = null) {
            return SortedGroupBy<TElement, TKey, TElement>(elements, keySelector, el => el, comparer);
        }

        /// <summary>
        /// Removes all list elements matching a predicate.
        /// Like <see cref="List{T}.RemoveAll"/>, but for <see cref="IList{T}"/>.
        /// </summary>
        /// <typeparam name="T">Element type of list.</typeparam>
        /// <param name="list"><see cref="IList{T}"/> instance to operate on.</param>
        /// <param name="match">Predicate to match against list elements.</param>
        /// <returns>Number of removed elements.</returns>
        public static int RemoveAll<T>(this IList<T> list, Predicate<T> match) {
            int endIndx = 0;
            for (int indx = 0; indx < list.Count; indx++) {
                var item = list[indx];
                if (!match(item)) {
                    if (endIndx < indx)
                        list[endIndx] = item;
                    endIndx++;
                }
            }
            int result = list.Count - endIndx;
            for (int lastIndx = list.Count - 1; lastIndx >= endIndx; lastIndx--) {
                list.RemoveAt(lastIndx);  // lastIndx is always equal to the current (list.Count - 1)
            }
            return result;
        }

        /// <summary>
        /// Appends a single element to an IEnumerable{T}.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="sequence">IEnumerable to append to.</param>
        /// <param name="toAppend">Element to append.</param>
        /// <returns></returns>
        public static IEnumerable<T> Append<T>(this IEnumerable<T> sequence, T toAppend) {
            if (sequence == null) throw new ArgumentNullException("sequence");
            return AppendImpl(sequence, toAppend);
        }

        static IEnumerable<T> AppendImpl<T>(IEnumerable<T> sequence, T toAppend) {
            foreach (T item in sequence) {
                yield return item;
            }
            sequence = null;
            yield return toAppend;
        }

        /// <summary>
        /// Prepends a single element to an IEnumerable{T}.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="sequence">IEnumerable to prepend to.</param>
        /// <param name="toPrepend">Element to prepend.</param>
        /// <returns></returns>
        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> sequence, T toPrepend) {
            if (sequence == null) throw new ArgumentNullException("sequence");
            return PrependImpl(sequence, toPrepend);
        }

        static IEnumerable<T> PrependImpl<T>(IEnumerable<T> sequence, T toPrepend) {
            yield return toPrepend;
            foreach (T item in sequence) {
                yield return item;
            }
        }
    }
}
