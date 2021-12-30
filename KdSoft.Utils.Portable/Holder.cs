using System.Diagnostics.CodeAnalysis;

namespace KdSoft.Utils
{
  /// <summary>
  /// Helper class that can serve as a reference holder for values that get set later.
  /// This is useful in asynchronous programming.
  /// </summary>
  /// <typeparam name="T">Type of return value.</typeparam>
  public class Holder<T> where T : class?
  {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    [AllowNull]
    public T Value { get; set; }
#else
    public T? Value { get; set; }
#endif
  }
}
