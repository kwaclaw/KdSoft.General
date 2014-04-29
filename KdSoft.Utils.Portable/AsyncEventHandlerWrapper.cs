using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace KdSoft.Utils
{
  public class AsyncEventHandlerWrapper<T> where T: EventArgs
  {
    public EventHandler<T> Handler { get; private set; }
    public TaskScheduler Scheduler { get; private set; }
    public Exception Error { get; private set; }

    public AsyncEventHandlerWrapper(EventHandler<T> handler) {
      if (handler == null)
        throw new ArgumentNullException("handler");
      this.Handler = handler;
      this.Scheduler = TaskScheduler.FromCurrentSynchronizationContext();
    }

    public void Invoke(object s, T e) {
      var task = new Task(() => {
        try {
          Handler(s, e);
          Error = null;
        }
        catch (Exception ex) {
          Error = ex;
        }
      });
      task.Start(Scheduler);
    }
  }
}
