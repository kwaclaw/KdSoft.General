using System;

namespace KdSoft.Utils
{
  /// <summary>
  /// Life cycle aware base class. Useful when one needs life cycle management.
  /// </summary>
  public class TimedLifeCycleAware: ILifeCycleAware<ITimedLifeCycle>
  {
    readonly TimedLifeCycle lifeCycle;

    public TimedLifeCycleAware(TimeSpan lifeSpan) {
      lifeCycle = new TimedLifeCycle(lifeSpan);
      lifeCycle.Ended += lifeCycle_Ended;
    }

    void lifeCycle_Ended(object sender, EventArgs e) {
      Close();
    }

    protected virtual void Close() { }

    #region ILifeCycleAware Members

    public ITimedLifeCycle GetLifeCycle() {
      return lifeCycle;
    }

    #endregion
  }
}
