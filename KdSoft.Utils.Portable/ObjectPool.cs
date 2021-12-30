using System;
using System.Collections.Concurrent;
using System.Threading;

namespace KdSoft.Utils
{
  // thread-safe
  public sealed class ObjectPool<T>: IDisposable where T : class?
  {
    ConcurrentBag<T> pool;
    Timer? idleTimer;
    int minCount;
    int maxIdleSteps;

    public ObjectPool() {
      pool = new ConcurrentBag<T>();
    }

    public ObjectPool(TimeSpan idleTimeout, int maxIdleSteps) : this() {
      this.MaxIdleSteps = maxIdleSteps;
      idleTimer = new Timer(IdleCallback, this, TimeSpan.Zero, idleTimeout);
    }

    static void IdleCallback(object? state) {
      var objPool = (ObjectPool<T>?)state;
      if (objPool == null)
        return;

      int steps = objPool.Count - objPool.minCount;
      if (steps > objPool.maxIdleSteps)
        steps = objPool.maxIdleSteps;
      for (; steps > 0; steps--) {
        if (!objPool.TryRemove())
          break;
      }
    }

    bool TryRemove() {
      T? obj;
      return pool.TryTake(out obj);
    }

    public T Borrow<O>() where O : T, new() {
      T? obj;
      if (!pool.TryTake(out obj))
        obj = new O();
      return obj;
    }

    public T Borrow(Func<T> creator) {
      T? obj;
      if (!pool.TryTake(out obj))
        obj = creator();
      return obj;
    }

    public void Return(T obj) {
      pool.Add(obj);
    }

    // sets MinCount, returns actual pool size
    public int EnsureCount<O>(int count) where O : T, new() {
      for (int ct = pool.Count; ct < count; ct++) {
        pool.Add(new O());
      }
      minCount = count;
      return pool.Count;
    }

    // sets MinCount, returns actual pool size
    public int EnsureCount<O>(int count, Func<T> creator) where O : T, new() {
      for (int ct = pool.Count; ct < count; ct++) {
        pool.Add(creator());
      }
      minCount = count;
      return pool.Count;
    }

    public int Count {
      get { return pool.Count; }
    }

    public int MinCount {
      get { return minCount; }
    }

    // periodically removes pooled objects, leaving at least MinCount objects in the pool
    public void SetIdleCleanup(TimeSpan idleTimeout, int maxIdleSteps) {
      this.MaxIdleSteps = maxIdleSteps;
      if (idleTimer == null)
        idleTimer = new Timer(IdleCallback, this, TimeSpan.Zero, idleTimeout);
      else
        idleTimer.Change(TimeSpan.Zero, idleTimeout);
    }

    public int MaxIdleSteps {
      get { return maxIdleSteps; }
      set {
        if (maxIdleSteps < 0)
          throw new ArgumentOutOfRangeException("MaxIdleSteps");
        maxIdleSteps = value;
      }
    }

    public void Dispose() {
      idleTimer?.Dispose();
    }
  }
}
