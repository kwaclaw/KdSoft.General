using System;
using System.Collections.Concurrent;

namespace KdSoft.Utils
{
  // thread-safe
  public class ObjectPool<T> where T : class
  {
    ConcurrentBag<T> pool;

    public ObjectPool() {
      pool = new ConcurrentBag<T>();
    }

    public T Borrow<O>() where O : T, new() {
      T obj;
      if (!pool.TryTake(out obj))
        obj = new O();
      return obj;
    }

    public T Borrow(Func<T> creator) {
      T obj;
      if (!pool.TryTake(out obj))
        obj = creator();
      return obj;
    }

    public void Return(T obj) {
      pool.Add(obj);
    }

    public int EnsureCount<O>(int count) where O : T, new() {
      for (int ct = pool.Count; ct < count; ct++) {
        pool.Add(new O());
      }
      return pool.Count;
    }

    public int EnsureCount<O>(int count, Func<T> creator) where O : T, new() {
      for (int ct = pool.Count; ct < count; ct++) {
        pool.Add(creator());
      }
      return pool.Count;
    }

    public int Count {
      get { return pool.Count; }
    }
  }
}
