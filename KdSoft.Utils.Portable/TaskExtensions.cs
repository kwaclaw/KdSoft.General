using System;
using System.Threading;
using System.Threading.Tasks;

namespace KdSoft.Utils
{
  public static class TaskEx
  {
    public static Task<TResult> FromResult<TResult>(TResult result) {
      var tcs = new TaskCompletionSource<TResult>();
      tcs.TrySetResult(result);
      return tcs.Task;
    }

    static void CheckFromAsyncOptions(TaskCreationOptions creationOptions, bool hasBeginMethod) {
      if (hasBeginMethod) {
        if ((creationOptions & TaskCreationOptions.LongRunning) != TaskCreationOptions.None) {
          throw new ArgumentOutOfRangeException("creationOptions");
        }
        if ((creationOptions & TaskCreationOptions.PreferFairness) != TaskCreationOptions.None) {
          throw new ArgumentOutOfRangeException("creationOptions");
        }
      }
      if ((creationOptions & ~(TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness)) != TaskCreationOptions.None) {
        throw new ArgumentOutOfRangeException("creationOptions");
      }
    }

    static void FromAsyncCoreLogic(IAsyncResult iar, Action<IAsyncResult> endMethod, TaskCompletionSource<object> tcs) {
      Exception exception = null;
      OperationCanceledException exception2 = null;
      try {
        endMethod(iar);
      }
      catch (OperationCanceledException exception3) {
        exception2 = exception3;
      }
      catch (Exception exception4) {
        exception = exception4;
      }
      finally {
        if (exception2 != null) {
          tcs.TrySetCanceled();
        }
        else if (exception != null) {
          tcs.TrySetException(exception);
        }
        else {
          tcs.TrySetResult(null);
        }
      }
    }

    public static Task FromAsync(
      this TaskFactory factory,
      Func<AsyncCallback, object, IAsyncResult> beginMethod,
      Action<IAsyncResult> endMethod,
      object state,
      CancellationToken cancelToken)
    {
      AsyncCallback callback = null;
      if (beginMethod == null) {
        throw new ArgumentNullException("beginMethod");
      }
      if (endMethod == null) {
        throw new ArgumentNullException("endMethod");
      }
      CheckFromAsyncOptions(factory.CreationOptions, true);
      TaskCompletionSource<object> tcs = new TaskCompletionSource<object>(state, factory.CreationOptions);
      if (cancelToken != CancellationToken.None)
        cancelToken.Register(() => tcs.TrySetCanceled());
      try {
        if (callback == null) {
          callback = delegate(IAsyncResult iar) { FromAsyncCoreLogic(iar, endMethod, tcs); };
        }
        beginMethod(callback, state);
      }
      catch {
        tcs.TrySetResult(null);
        throw;
      }
      return tcs.Task;
    }

    static void FromAsyncCoreLogic<TResult>(
      IAsyncResult iar,
      Func<IAsyncResult, TResult> endMethod,
      TaskCompletionSource<TResult> tcs)
    {
      Exception exception = null;
      OperationCanceledException exception2 = null;
      TResult result = default(TResult);
      try {
        result = endMethod(iar);
      }
      catch (OperationCanceledException exception3) {
        exception2 = exception3;
      }
      catch (Exception exception4) {
        exception = exception4;
      }
      finally {
        if (exception2 != null) {
          tcs.TrySetCanceled();
        }
        else if (exception != null) {
          tcs.TrySetException(exception);
        }
        else {
          tcs.TrySetResult(result);
        }
      }
    }

    public static Task<TResult> FromAsync<TResult>(
      this TaskFactory<TResult> factory,
      Func<AsyncCallback, object, IAsyncResult> beginMethod,
      Func<IAsyncResult, TResult> endMethod,
      object state,
      CancellationToken cancelToken)
    {
      AsyncCallback callback = null;
      if (beginMethod == null) {
        throw new ArgumentNullException("beginMethod");
      }
      if (endMethod == null) {
        throw new ArgumentNullException("endMethod");
      }
      CheckFromAsyncOptions(factory.CreationOptions, true);
      TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>(state, factory.CreationOptions);
      if (cancelToken != CancellationToken.None)
        cancelToken.Register(() => tcs.TrySetCanceled());
      try {
        if (callback == null) {
          callback = delegate(IAsyncResult iar) { FromAsyncCoreLogic(iar, endMethod, tcs); };
        }
        beginMethod(callback, state);
      }
      catch {
        tcs.TrySetResult(default(TResult));
        throw;
      }
      return tcs.Task;
    }

    /// <summary>Cancels the <see cref="T:System.Threading.CancellationTokenSource" /> after the specified duration.</summary>
    /// <param name="source">The CancellationTokenSource.</param>
    /// <param name="dueTime">The due time in milliseconds for the source to be canceled.</param>
    public static void CancelAfter(this CancellationTokenSource source, int dueTime) {
      if (source == null) {
        throw new NullReferenceException();
      }
      if (dueTime < -1) {
        throw new ArgumentOutOfRangeException("dueTime");
      }
      Timer timer = new Timer(delegate(object self) {
        ((IDisposable)self).Dispose();
        try {
          source.Cancel();
        }
        catch (ObjectDisposedException) {
        }
      }, null, dueTime, -1);
    }

    public static Task<T> Await<T>(
      TaskEnumerator<T> getTaskEnumerator,
      CancellationToken cancelToken = new CancellationToken(),
      TaskScheduler scheduler = null) 
    {
      var awaiter = new TaskAwaiter<T>(getTaskEnumerator, cancelToken, scheduler);
      return awaiter.Await();
    }

    public static Task<T> Await<T>(
      TaskEnumerable<T> getTaskEnumerable,
      CancellationToken cancelToken = new CancellationToken(),
      TaskScheduler scheduler = null) 
    {
      var awaiter = new TaskAwaiter<T>(getTaskEnumerable, cancelToken, scheduler);
      return awaiter.Await();
    }
  }
}
