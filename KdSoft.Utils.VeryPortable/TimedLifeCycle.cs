using System;

namespace KdSoft.Utils
{
  /// <summary>
  /// Implementation of <see cref="ITimedLifeCycle"/>.
  /// </summary>
  public class TimedLifeCycle: ITimedLifeCycle
  {
    DateTime lastUsed;
    TimeSpan lifeTime;
    bool isEnded;

    public TimedLifeCycle(TimeSpan lifeTime) {
      this.lifeTime = lifeTime;
      lastUsed = DateTime.UtcNow;
    }

    /// <summary>
    /// Event that is triggered when the life-cycle has ended. Useful for clean-up purposes.
    /// </summary>
    public event EventHandler Ended;

    #region ITimedLifeCycle Members

    public TimeSpan LifeSpan {
      get { return lifeTime; }
    }

    public DateTime? Used() {
      lock (this) {
        if (isEnded)
          return null;
        lastUsed = DateTime.UtcNow;
        return lastUsed;
      }
    }

    public DateTime LastUsed {
      get { lock (this) return lastUsed; }
    }

    #endregion

    #region ILifeCycle Members

    public bool CheckAlive() {
      bool isEnding;
      lock (this) {
        if (isEnded)
          return false;
        var unused = DateTime.UtcNow - lastUsed;
        isEnding = unused > lifeTime;
        if (isEnding)
          isEnded = true;
      }
      if (isEnding) {
        var ended = Ended;
        if (ended != null)
          Ended(this, EventArgs.Empty);
        return false;
      }
      return true;
    }

    public void Terminate() {
      lock (this) {
        if (isEnded)
          return;
        isEnded = true;
      }
      var ended = Ended;
      if (ended != null)
        Ended(this, EventArgs.Empty);
    }

    #endregion
  }
}
