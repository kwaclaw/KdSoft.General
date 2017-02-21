using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KdSoft.Utils
{
    /// <summary>
    /// <see cref="LinkedList{T}"/> extension methods.
    /// </summary>
    public static class LinkedListExtensions
    {
        /// <summary>
        /// Enumerates the <see cref="LinkedListNode{Task}"/> instances of the linked list.
        /// </summary>
        /// <typeparam name="T">Type of list items.</typeparam>
        /// <param name="list"><see cref="LinkedList{T}"/> to enumerate the nodes from.</param>
        /// <returns><see cref="IEnumerable{LinkedListNode{T}}"/>.</returns>
        public static IEnumerable<LinkedListNode<T>> Nodes<T>(this LinkedList<T> list) {
            var node = list.First;
            while (node != null) {
                yield return node;
                node = node.Next;
            }
        }

        /// <summary>
        /// Rotates <see cref="LinkedList{T}"/> right (first to last). 
        /// </summary>
        /// <typeparam name="T">Type of list items.</typeparam>
        /// <param name="list"><see cref="LinkedList{T}"/> to rotate.</param>
        /// <param name="rotationSteps">Number of rotation steps to perform.</param>
        public static void RotateRight<T>(this LinkedList<T> list, int rotationSteps) {
            for (int indx = 0; indx < rotationSteps; indx++) {
                var first = list.First;
                list.RemoveFirst();
                list.AddLast(first);
            }
        }

        /// <summary>
        /// Rotates <see cref="LinkedList{T}"/> left (last to first). 
        /// </summary>
        /// <typeparam name="T">Type of list items.</typeparam>
        /// <param name="list"><see cref="LinkedList{T}"/> to rotate.</param>
        /// <param name="rotationSteps">Number of rotation steps to perform.</param>
        public static void RotateLeft<T>(this LinkedList<T> list, int rotationSteps) {
            for (int indx = 0; indx < rotationSteps; indx++) {
                var last = list.Last;
                list.RemoveLast();
                list.AddFirst(last);
            }
        }
    }
}
