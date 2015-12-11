using System;
using System.ComponentModel;

namespace KdSoft.Utils
{
  public static class AsyncUtils
  {
    /// <summary>
    /// Wraps an event handler for asynchronous invokation on the calling thread.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static EventHandler WrapAsync(EventHandler handler)
    {
      var wrapper = new AsyncEventHandlerWrapper<EventArgs>(new EventHandler<EventArgs>(handler));
      return wrapper.Invoke;
    }

    /// <summary>
    /// Wraps an event handler for asynchronous invokation on the calling thread.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static EventHandler<T> WrapAsync<T>(EventHandler<T> handler) where T: EventArgs
    {
      var wrapper = new AsyncEventHandlerWrapper<T>(handler);
      return wrapper.Invoke;
    }

    /// <summary>
    /// Wraps a property changed event handler for asynchronous invokation on the calling thread.
    /// </summary>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static PropertyChangedEventHandler WrapAsync(PropertyChangedEventHandler handler)
    {
      var wrapper = new AsyncEventHandlerWrapper<PropertyChangedEventArgs>(new EventHandler<PropertyChangedEventArgs>(handler));
      return wrapper.Invoke;
    }
  }
}
