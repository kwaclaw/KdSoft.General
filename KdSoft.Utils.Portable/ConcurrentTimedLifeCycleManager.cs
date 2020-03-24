using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace KdSoft.Utils
{
  /// <summary>
  /// Manages and tracks the life-cycle of objects implementing <see cref="ILifeCycleAware{ITimedLifeCycle}"/>.
  /// When an object's life-cycle has expired and no other objects have a reference to it then
  /// it will be removed from the internal object map and consequently will be available for
  /// garbage collection. On the other hand, an object will be kept un-collected as long
  /// as its life-cycle has not expired, which is useful for stateful server objects.
  /// </summary>
  /// <typeparam name="K">Type of key that identifies tracked objects.</typeparam>
  /// <typeparam name="O">Type of <see cref="ILifeCycleAware{ITimedLifeCycle}"/> objects being tracked.</typeparam>
  public class ConcurrentTimedLifeCycleManager<K, O>: IDisposable where O : ILifeCycleAware<ITimedLifeCycle>
  {
    readonly ConcurrentDictionary<K, O> objectMap;
    Timer lifeCycleTimer;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="reapPeriod">Time interval for periodic life-cycle checking.</param>
    /// <param name="comparer">Optional, custom key equality comparer.</param>
    public ConcurrentTimedLifeCycleManager(TimeSpan reapPeriod, IEqualityComparer<K> comparer = null) {
      this.lifeCycleTimer = new Timer(LifeCycleHandler, this, reapPeriod, reapPeriod);
      if (comparer is null)
        objectMap = new ConcurrentDictionary<K, O>();
      else
        objectMap = new ConcurrentDictionary<K, O>(comparer);
    }

    // must not throw exceptions
    static void LifeCycleHandler(object state) {
      var lfMgr = (ConcurrentTimedLifeCycleManager<K, O>)state;
      var termHandler = lfMgr.Terminated;
      foreach (var objectEntry in lfMgr.objectMap) {
        var lc = objectEntry.Value.GetLifeCycle();
        if (!lc.CheckAlive()) {  // not supposed to throw exception
          O value;
          if (lfMgr.objectMap.TryRemove(objectEntry.Key, out value))
            termHandler?.Invoke(lfMgr, new EventArgs<K, O>(objectEntry.Key, value));
        }
      }
    }

    void CheckDisposed() {
      if (lifeCycleTimer == null)
        throw new ObjectDisposedException(GetType().Name);
    }

    /// <summary>
    /// Called when an object is terminated, passing key and instance.
    /// </summary>
    /// <remarks>Must not throw exceptions.</remarks>
    public event EventHandler<EventArgs<K, O>> Terminated;

    /// <summary>
    /// Adds new object to life-cycle management and tracking. Does not check the life-cycle of the new object.
    /// </summary>
    /// <param name="key">Key by which object is identified.</param>
    /// <param name="obj">Object instance to track.</param>
    /// <returns><c>true</c> if the object was added successfuly, <c>false</c> if an entry for the key already exists.</returns>
    public bool TryAdd(K key, O obj) {
      CheckDisposed();
      if (obj == null)
        throw new ArgumentNullException("obj");
      return objectMap.TryAdd(key, obj);
    }

    struct ReplacementProvider
    {
      readonly O newObj;

      public ReplacementProvider(O newObj) {
        this.newObj = newObj;
      }

#pragma warning disable S1172 // Unused method parameters should be removed
      public O GetValue(K key, O oldObj) {
#pragma warning restore S1172 // Unused method parameters should be removed
        var lc = oldObj.GetLifeCycle();
        lc.Terminate();
        return newObj;
      }
    }

    /// <summary>
    /// Adds new object to life-cycle management and tracking, or replaces existing one.
    /// Does not check the life-cycle of the new object. Terminates existing object's life-cycle, if applicable.
    /// </summary>
    /// <param name="key">Key by which object can be identified.</param>
    /// <param name="obj">New object instance to track.</param>
    public void AddOrUpdate(K key, O obj) {
      CheckDisposed();
      if (obj == null)
        throw new ArgumentNullException("obj");
      var replacer = new ReplacementProvider(obj);
      objectMap.AddOrUpdate(key, obj, replacer.GetValue);
    }

    /// <summary>
    /// Adds new object to life-cycle management and tracking, or replaces existing one.
    /// Does not check the life-cycle of the new object. Does *not* check, terminate or restart
    /// the existing object's life-cycle, unless performed in "updateObj" callback.
    /// </summary>
    /// <param name="key">Key by which object is identified.</param>
    /// <param name="obj">Object instance to track.</param>
    /// <param name="updateObj">The function used to generate a new value for an existing key
    /// based on the  key's existing value. This function *may* return the existing object
    /// and check or restart the existing object's life-cycle.</param>
    /// <returns>The new value for the key.</returns>
    public O AddOrUpdate(K key, O obj, Func<K, O, O> updateObj) {
      CheckDisposed();
      if (obj == null)
        throw new ArgumentNullException("obj");
      if (updateObj == null)
        throw new ArgumentNullException("updateObj");
      return objectMap.AddOrUpdate(key, obj, updateObj);
    }

    struct ValueProvider
    {
      readonly O newObj;

      public ValueProvider(O newObj) {
        this.newObj = newObj;
      }

#pragma warning disable S1172 // Unused method parameters should be removed
      public O GetValue(K key, O oldObj) {
#pragma warning restore S1172 // Unused method parameters should be removed
        var lc = oldObj.GetLifeCycle();
        if (lc.CheckAlive()) {
          lc.Used(); // restart life-cycle
          return oldObj;
        }
        lc.Terminate();
        return newObj;
      }
    }

    /// <summary>
    /// Returns "live" object matching the key. This can be an existing object as long as it is still "alive",
    /// or the new object passed as argument, in which case the new object would be added to life-cycle management and tracking.
    /// Makes sure that life-cycle of existing object gets restarted if applicable, calling <see cref="ITimedLifeCycle.Used"/>.
    /// Calls <see cref="ILifeCycle.Terminate"/> on existing object if it's life-cycle has ended, and removes it.
    /// </summary>
    /// <param name="key">Key by which object can be identified.</param>
    /// <param name="obj">New object instance to track. Only used if none exists for the key.</param>
    /// <returns>"live" object, matching the key.</returns>
    public O GetOrAdd(K key, O obj) {
      CheckDisposed();
      if (obj == null)
        throw new ArgumentNullException("obj");
      var provider = new ValueProvider(obj);
      return objectMap.AddOrUpdate(key, obj, provider.GetValue);
    }

    struct FuncProvider
    {
      readonly Func<K, O> getNew;

      public FuncProvider(Func<K, O> getNew) {
        this.getNew = getNew;
      }

      // returns existing value if still alive
      public O GetValue(K key, O oldObj) {
        var lc = oldObj.GetLifeCycle();
        if (lc.CheckAlive()) {
          lc.Used(); // restart life-cycle
          return oldObj;
        }
        lc.Terminate();
        return getNew(key);
      }
    }

    /// <summary>
    /// Returns "live" object matching the key. This can be an existing object as long as it is still "alive", or a new object
    /// returned from the "getNew" callback, in which case the new object would be added to life-cycle management and tracking.
    /// Makes sure that life-cycle of existing object gets restarted if applicable, calling <see cref="ITimedLifeCycle.Used"/>.
    /// Calls <see cref="ILifeCycle.Terminate"/> on existing object if it's life-cycle has ended, and removes it.
    /// </summary>
    /// <param name="key">Key by which object can be identified.</param>
    /// <param name="getNew">Delegate to call when new object must be retrieved.</param>
    /// <returns>"live" object, matching the key.</returns>
    public O GetOrAdd(K key, Func<K, O> getNew) {
      CheckDisposed();
      if (getNew == null)
        throw new ArgumentNullException("getNew");
      var provider = new FuncProvider(getNew);
      return objectMap.AddOrUpdate(key, getNew, provider.GetValue);
    }

    /// <summary>
    /// Returns "live" object identified by key from object map. Terminates and removes object from map if it is "dead."
    /// Makes sure that life-cycle of object gets restarted if it is "alive", calling <see cref="ITimedLifeCycle.Used"/>.
    /// </summary>
    /// <param name="key">Key by which object can be identified.</param>
    /// <param name="obj">Object to return if it is "alive", <c>null</c> otherwise.</param>
    /// <returns><c>true</c> if "live" object was found, <c>false</c> otherwise.</returns>
    public bool TryGetValue(K key, out O obj) {
      CheckDisposed();
      if (objectMap.TryGetValue(key, out obj)) {
        var lc = obj.GetLifeCycle();
        if (lc.CheckAlive()) {
          lc.Used();
          return true;
        }
        lc.Terminate();
        obj = default(O);
        //TODO threading issue: what if new object for same key was added since we retrieved it
        O currObj;
        objectMap.TryRemove(key, out currObj);
      }
      return false;
    }

    /// <summary>
    /// Removes object from life-cycle management and tracking. Does *not* check if object is "alive".
    /// </summary>
    /// <param name="key">Key by which object is identified.</param>
    /// <param name="obj">Object instance that was removed, if applicable.</param>
    /// <returns><c>true</c> if object matching the key was found and removed, <c>false</c> otherwise.</returns>
    public bool TryRemove(K key, out O obj) {
      CheckDisposed();
      return objectMap.TryRemove(key, out obj);
    }

    /// <summary>
    /// Terminates life cycle for a given object and removes it from object map immediately.
    /// </summary>
    /// <param name="key">Key by which object can be identified.</param>
    /// <returns><c>true</c> if object was found and terminated, <c>false</c> otherwise.</returns>
    public bool TryTerminate(K key) {
      CheckDisposed();
      O obj;
      if (objectMap.TryRemove(key, out obj)) {
        var lc = obj.GetLifeCycle();
        lc.Terminate();
        return true;
      }
      return false;
    }

    /// <summary>
    /// Terminates all object lifecycles managed by this instance. Does *not* remove them from Map.
    /// </summary>
    public void TerminateAll() {
      CheckDisposed();
      foreach (var entry in objectMap) {
        var lc = entry.Value.GetLifeCycle();
        lc.Terminate();
      }
    }

    /// <summary>
    /// Returns snapshot of object entries.
    /// </summary>
    public KeyValuePair<K, O>[] GetSnapshot() {
      CheckDisposed();
      return objectMap.ToArray();
    }

    #region IDisposable Members

    protected virtual void Dispose(bool disposing) {
      if (disposing) {
        var lt = lifeCycleTimer;
        if (lt != null) {
          lifeCycleTimer = null;
          lt.Dispose();
        }
      }
    }

    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    #endregion
  }
}
