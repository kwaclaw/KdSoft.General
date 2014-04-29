using System;
using System.Threading.Tasks;

namespace KdSoft.Utils
{
  /// <summary>
  /// Wrapper for <see cref="Task"/> that can be used to implement the APM pattern.
  /// </summary>
  /// <typeparam name="T">Task sub-type.</typeparam>
  /// <typeparam name="C">Type of context. Useful for error reporting.</typeparam>
  /// <remarks>See this example: <code>
  ///public IAsyncResult BeginGetFile(string directoryUrl, string fileName, bool deleteAfter, AsyncCallback callback, object asyncState) {
  ///  var readTask = fileNode.GetFile(directoryUrl, fileName, deleteAfter);
  ///  var asyncResult = new AsyncTaskResult<Task<byte[]>, string>(readTask, fileName, asyncState);
  ///  var completedTask = readTask.ContinueWith((rt) => callback(asyncResult));
  ///  return asyncResult;
  ///}
  ///public byte[] EndGetFile(IAsyncResult ar) {
  ///  var taskResult = (AsyncTaskResult<Task<byte[]>, string>)ar;
  ///  if (taskResult.Task.IsCompleted)
  ///    return taskResult.Task.Result;
  ///  else if (taskResult.Task.IsFaulted) {
  ///    Exception ex = taskResult.Task.Exception;
  ///    var reason = new FaultReason(string.Format("Error retrieving file '{0}'.", taskResult.Context));
  ///    throw new FaultException(reason);
  ///  }
  ///  else
  ///    throw new FaultException(string.Format("Cancelled retrieval of file '{0}'.", taskResult.Context));
  ///}
  ///</code></remarks>
  public class AsyncTaskResult<T, C>: IAsyncResult where T: Task
  {
    T task;
    C context;
    object asyncState;

    public AsyncTaskResult(T task, C context, object asyncState) {
      this.task = task;
      this.context = context;
      this.asyncState = asyncState;
    }

    public T Task {
      get { return task; }
    }

    public C Context {
      get { return context; }
    }

    #region IAsyncResult Members

    public object AsyncState {
      get { return asyncState; }
    }

    public System.Threading.WaitHandle AsyncWaitHandle {
      get { return ((IAsyncResult)task).AsyncWaitHandle; }
    }

    public bool CompletedSynchronously {
      get { return ((IAsyncResult)task).CompletedSynchronously; }
    }

    public bool IsCompleted {
      get { return task.IsCompleted; }
    }

    #endregion
  }
}
