using System.Collections.Generic;
using System.Linq;

namespace KdSoft.Utils
{
  public class Grouping<TKey, TElement>: IGrouping<TKey, TElement>
  {
    TKey key;
    IEnumerable<TElement> elements;

    static readonly TElement[] defaultEnumerable = new TElement[0];

    public Grouping(TKey key, IEnumerable<TElement> elements = null) {
      Initialize(key, elements);
    }

    // Use this to re-use the instance, if applicable.
    public void Initialize(TKey key, IEnumerable<TElement> elements = null) {
      this.key = key;
      this.elements = elements ?? defaultEnumerable;
    }

    public TKey Key { get { return key; } }

    public IEnumerator<TElement> GetEnumerator() {
      return elements.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }
  }
}
