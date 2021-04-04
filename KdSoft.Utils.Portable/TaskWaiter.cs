using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace KdSoft.Utils
{
  /// <summary>
  /// Allows to wait for, or be notified of, the completion of a dynamic set of tasks.
  /// </summary>
  public class TaskWaiter: IDisposable
  {
    CountdownEvent _ce;
    bool _stopOnFirstError;
    int _doneAdding;
    ConcurrentQueue<Exception> _errors;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="stopOnFirstError">Indicates if adding tasks should be stopped after the first error. This allows
    /// for an earlier return from the wait in cases when no more tasks should be run after the first error.</param>
    public TaskWaiter(bool stopOnFirstError = false) {
      _ce = new CountdownEvent(1);
      this._stopOnFirstError = stopOnFirstError;
      _doneAdding = 0;
      _errors = new ConcurrentQueue<Exception>();
    }

    void Signal() {
      if (_ce.Signal())
        HandleCompleted();
    }

    void HandleCompleted() {
      var onCompleted = OnCompleted;
      if (onCompleted != null) {
        AggregateException aggEx = null;
        if (!_errors.IsEmpty)
          aggEx = new AggregateException(_errors);
        onCompleted(this, new EventArgs<Exception>(aggEx));
      }
    }

    /// <summary>
    /// Indicates if more tasks can be added or not.
    /// </summary>
    public bool AddingComplete {
      get {
#if NET40 || NET403
        Thread.MemoryBarrier();
        bool doneAdding = _doneAdding != 0;
        Thread.MemoryBarrier();
        return doneAdding;
#else
        Interlocked.MemoryBarrier();
        bool doneAdding = _doneAdding != 0;
        Interlocked.MemoryBarrier();
        return doneAdding;
#endif
      }
    }

    /// <summary>
    /// Event that notifies of the completion of all tasks.
    /// An alternative to using <see cref="Wait"/>.
    /// </summary>
    public event EventHandler<EventArgs<Exception>> OnCompleted;

    /// <summary>
    /// Adds a new task to be waited for.
    /// </summary>
    /// <param name="t">Task to wait for.</param>
    /// <returns><c>true</c> if the task could be added, <c>false</c> otherwise.</returns>
    public bool Add(Task t) {
      if (t == null)
        throw new ArgumentNullException("t");
      if (AddingComplete)
        return false;

      _ce.AddCount();
      t.ContinueWith(ct => {
        if (ct.Exception != null) {
          _errors.Enqueue(ct.Exception);
          if (_stopOnFirstError)
            CompleteAdding();
        }
        Signal();
      });
      return true;
    }

    /// <summary>
    /// Inidicates that noe more tasks can be added. It is only after this call that the wait may finish,
    /// or that the <see cref="OnCompleted"/> handler may get called.
    /// </summary>
    public void CompleteAdding() {
      int doneAdding = Interlocked.CompareExchange(ref _doneAdding, 0xFF, 0);
      if (doneAdding != 0)
        return;
      Signal();
    }

    /// <summary>
    /// Waits for all tasks to finish. Implicity calls <see cref="CompleteAdding"/>.
    /// An alternative to using the <see cref="OnCompleted"/> event.
    /// </summary>
    /// <param name="ct">Cancellation token, to cancel wait.</param>
    public void Wait(CancellationToken ct) {
      CompleteAdding();
      _ce.Wait(ct);
      if (!_errors.IsEmpty)
        throw new AggregateException(_errors);
    }

    #region IDisposable Members

    protected virtual void Dispose(bool disposing) {
      if (disposing) {
        var ce = _ce;
        if (ce != null) {
          ce.Dispose();
        }
      }
    }

    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    #endregion
  }
}
