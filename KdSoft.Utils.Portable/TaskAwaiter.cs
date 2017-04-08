using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KdSoft.Utils
{
  public delegate IEnumerator<Task> TaskEnumerator<T>(TaskCompletionSource<T> taskCompletion, CancellationToken cancelToken);
  public delegate IEnumerable<Task> TaskEnumerable<T>(TaskCompletionSource<T> taskCompletion, CancellationToken cancelToken);

  public class TaskAwaiter<T>
  {
    protected readonly TaskCompletionSource<T> taskCompletion;
    protected readonly CancellationToken cancelToken;
    TaskScheduler scheduler;
    IEnumerator<Task> taskEnumerator;
    Task lastTask;

    public TaskAwaiter(
      TaskEnumerator<T> getTaskEnumerator,
      CancellationToken cancelToken = new CancellationToken(),
      TaskScheduler scheduler = null
    ) {
      this.taskCompletion = new TaskCompletionSource<T>();
      this.cancelToken = cancelToken;
      this.scheduler = scheduler;
      this.taskEnumerator = getTaskEnumerator(this.taskCompletion, cancelToken);
    }

    public TaskAwaiter(
      TaskEnumerable<T> getTaskEnumerable,
      CancellationToken cancelToken = new CancellationToken(),
      TaskScheduler scheduler = null
    ) {
      this.taskCompletion = new TaskCompletionSource<T>();
      this.cancelToken = cancelToken;
      this.scheduler = scheduler;
      this.taskEnumerator = getTaskEnumerable(this.taskCompletion, cancelToken).GetEnumerator();
    }

    void RunTask(Task tsk) {
      if (cancelToken.IsCancellationRequested) {
        taskCompletion.TrySetCanceled();
        return;
      }
      try {
        if (taskEnumerator.MoveNext()) {
          lastTask = taskEnumerator.Current;
          lastTask.ContinueWith(RunTask, scheduler ?? TaskScheduler.Current);
        }
        else {
          if (taskCompletion.Task.Status == TaskStatus.WaitingForActivation) {
            if (lastTask == null) {
              taskCompletion.TrySetResult(default(T));
            }
            else if (lastTask.IsFaulted)
              taskCompletion.TrySetException(lastTask.Exception);
            else if (lastTask.IsCanceled)
              taskCompletion.TrySetCanceled();
            else {
              var resultTask = lastTask as Task<T>;
              if (resultTask != null)
                taskCompletion.TrySetResult(resultTask.Result);
              else
                taskCompletion.TrySetResult(default(T));
            }
          }
          taskEnumerator.Dispose();
        }
      }
      catch (Exception ex) {
        taskCompletion.TrySetException(ex);
        taskEnumerator.Dispose();
      }
    }

    public Task<T> Await() {
      RunTask(null);
      return taskCompletion.Task;
    }
  }
}
