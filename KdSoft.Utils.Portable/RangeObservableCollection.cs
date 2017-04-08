using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace KdSoft.Utils
{
  /// <summary>
  /// <see cref="ObservableCollection{T}"/> subclass that adds range modification methods.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class RangeObservableCollection<T>: ObservableCollection<T>
  {
    ListSegment<T> insertedItems = new ListSegment<T>();
    List<T> removedItems = new List<T>();

    public void InsertRange(int index, IEnumerable<T> items) {
      if (items == null)
        return;
      CheckReentrancy();
      int startIndex = index;
      foreach (T current in items) {
        base.Items.Insert(index++, current);
      }
      this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
      this.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
      var newItems = insertedItems.Initialize(base.Items, startIndex, index - startIndex);
      var eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, startIndex);
      this.OnCollectionChanged(eventArgs);
    }

    public void AddRange(IEnumerable<T> items) {
      InsertRange(Count, items);
    }

    public void RemoveRange(int index, int count) {
      if (count == 0) {
        return;
      }
      CheckReentrancy();
      removedItems.Clear();
      while (count-- > 0) {
        removedItems.Add(base.Items[index]);
        base.Items.RemoveAt(index);
      }
      this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
      this.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
      var eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems, index);
      this.OnCollectionChanged(eventArgs);
    }
  }
}
