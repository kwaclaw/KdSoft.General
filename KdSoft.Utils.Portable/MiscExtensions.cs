using System;
using System.Collections.Generic;
using System.Text;

namespace KdSoft.Utils
{
    public static class MiscExtensions
    {
        /// <summary>
        /// Removes trailing zeros from decimal value. Useful to prevent database parameter 'out of range' errors.
        /// </summary>
        /// <param name="value">Decimal value with trailing zeros.</param>
        /// <returns>Decimal with trailing zeros removed.</returns>
        public static decimal Normalize(this decimal value) {
            return value / 1.000000000000000000000000000000000m;
        }

        /// <summary>
        /// Checks if a dictionary contains a given key-value pair. Useful in expressions where <c>TryGetValue</c> cannot be used.
        /// </summary>
        /// <typeparam name="TKey">Type of key.</typeparam>
        /// <typeparam name="TValue">Type of value.</typeparam>
        /// <param name="dict">Dictionary to check.</param>
        /// <param name="key">Key to use.</param>
        /// <param name="value">Value to use.</param>
        /// <returns><c>true</c> if the dictionary contains the specified key-value pair, <c>false</c> otherwise.</returns>
        public static bool HasKeyValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value) where TKey : notnull {
            TValue? entry;
            if (dict.TryGetValue(key, out entry)) {
                return object.Equals(value, entry);
            }
            else
                return false;
        }

        /// <summary>
        /// Combines all nested exeption messages of an <see cref="AggregateException"/>, each starting at a new line.
        /// </summary>
        public static string CombineMessages(this AggregateException aggex) {
            var exs = aggex.Flatten().InnerExceptions;
            if (exs.Count == 0)
                return string.Empty;
            int totalLength = 0;
            for (int indx = 0; indx < exs.Count; indx++) {
                var ex = exs[indx];
                if (ex.Message != null)
                    totalLength += ex.Message.Length;
            }
            totalLength += Environment.NewLine.Length * exs.Count;
            var sb = new StringBuilder(totalLength);
            for (int indx = 0; indx < exs.Count; indx++) {
                var ex = exs[indx];
                if (ex.Message != null)
                    sb.AppendLine(ex.Message);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Like the null-coalescing operator, but also works for empty strings.
        /// </summary>
        public static string IfNullOrEmpty(this string? str, string defaultValue) {
            return string.IsNullOrEmpty(str) ? defaultValue : str!;
        }

        /// <summary>
        /// Like the null-coalescing operator, but also works for empty strings and white space.
        /// </summary>
        public static string IfNullOrWhiteSpace(this string? str, string defaultValue) {
            return string.IsNullOrWhiteSpace(str) ? defaultValue : str!;
        }
    }
}
