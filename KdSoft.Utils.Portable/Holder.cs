using System;

namespace KdSoft.Utils
{
  /// <summary>
  /// Helper class that can serve as a reference holder for values that get set later.
  /// This is useful in asynchronous programming.
  /// </summary>
  /// <typeparam name="T">Type of return value.</typeparam>
  public class Holder<T> where T : class
  {
    public T Value { get; set; }
  }
}
