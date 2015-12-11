using System;

namespace KdSoft.Utils
{
  // This class goes nicely with EventHandler<T>. Strangely, it does not exist in the framework.
  public class EventArgs<T> : EventArgs
  {
    public EventArgs(T value) {
      Value = value;
    }
    public T Value { get; private set; }

    public new static readonly EventArgs<T> Empty =
        new EventArgs<T>(default(T));
  }

  public class EventArgs<T, U> : EventArgs<T>
  {
    public EventArgs(T value, U otherValue) : base(value) {
      OtherValue = otherValue;
    }
    public U OtherValue { get; private set; }

    public new static readonly EventArgs<T, U> Empty =
        new EventArgs<T, U>(default(T), default(U));
  }
}
