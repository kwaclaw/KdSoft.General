using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace KdSoft.Utils
{
  /// <summary>
  /// <see cref="ObservableCollection{T}"/> subclass that adds range modification methods.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <remarks>This class is re-entrant. Because of this, any bulk operation will trigger a notification of the 'Reset' type.</remarks>
  public class BulkObservableCollection<T>: ObservableCollection<T>
  {
    int bulkOperationCount;
    bool collectionChangedDuringBulkOperation;

    public void InsertRange(int index, IEnumerable<T> items) {
      if (items == null) {
        return;
      }
      try {
        this.BeginBulkOperation();
        foreach (T current in items) {
          base.InsertItem(index, current);
        }
      }
      finally {
        this.EndBulkOperation();
      }
      this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public void AddRange(IEnumerable<T> items) {
      InsertRange(Count, items);
    }

    public void RemoveRange(int index, int count) {
      if (count == 0) {
        return;
      }
      try {
        this.BeginBulkOperation();
        while (count-- > 0) {
          base.RemoveAt(index);
        }
      }
      finally {
        this.EndBulkOperation();
      }
      this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public void BeginBulkOperation() {
      this.bulkOperationCount++;
    }

    public void EndBulkOperation() {
      if (this.bulkOperationCount > 0 && --this.bulkOperationCount == 0 && this.collectionChangedDuringBulkOperation) {
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        this.collectionChangedDuringBulkOperation = false;
      }
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
      if (this.bulkOperationCount == 0) {
        base.OnCollectionChanged(e);
        return;
      }
      this.collectionChangedDuringBulkOperation = true;
    }
  }
}
