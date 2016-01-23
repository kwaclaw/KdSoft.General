using System;
using System.Collections.Generic;
using System.Threading;

namespace KdSoft.Utils
{
    /// <summary>
    /// Manages and tracks the life-cycle of objects implementing <see cref="ILifeCycleAware"/>.
    /// When an object's life-cycle has expired and no other objects have a reference to it then
    /// it will be removed from the internal object map and consequently will be available for
    /// garbage collection. On the other hand, an object will be kept un-collected as long
    /// as its life-cycle has not expired, which is useful for stateful server objects.
    /// </summary>
    /// <typeparam name="K">Type of key that identifies tracked objects.</typeparam>
    /// <typeparam name="O">Type of <see cref="ILifeCycleAware"/> objects being tracked.</typeparam>
    /// <remarks>Not thread-safe by default, external synchronization should be performed against the
    /// <c>syncObj</c> parameter passed to the constructor.</remarks>
    public class TimedLifeCycleManager<K, O>: IDisposable where O : ILifeCycleAware<ITimedLifeCycle>
    {
        public readonly object SyncObj;
        Dictionary<K, O> objectMap;
        Timer lifeCycleTimer;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="reapPeriod">Time interval for periodic life-cycle checking.</param>
        /// <param name="syncObj">Synchronization object for external locking. The internal timer handler
        /// will lock on this object when calling the <see cref="Terminated"/> event handler.</param>
        public TimedLifeCycleManager(TimeSpan reapPeriod, object syncObj) {
            this.SyncObj = syncObj ?? new object();
            this.lifeCycleTimer = new Timer(LifeCycleHandler, this, reapPeriod, reapPeriod);
            objectMap = new Dictionary<K, O>();
        }

        // must not throw exceptions
        static void LifeCycleHandler(object state) {
            var lfMgr = (TimedLifeCycleManager<K, O>)state;
            var keyList = new List<K>();
            lock (lfMgr.SyncObj) {
                foreach (var objectEntry in lfMgr.objectMap) {
                    var lc = objectEntry.Value.GetLifeCycle();
                    if (!lc.CheckAlive())  // not supposed to throw exception
                        keyList.Add(objectEntry.Key);
                }
                for (int indx = 0; indx < keyList.Count; indx++)
                    lfMgr.objectMap.Remove(keyList[indx]);
                var termHandler = lfMgr.Terminated;
                if (termHandler != null)
                    termHandler(lfMgr, new EventArgs<IList<K>>(keyList));
            }
        }

        void CheckDisposed() {
            if (lifeCycleTimer == null)
                throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// Called when a list of objects is terminated.
        /// </summary>
        /// <remarks>Must not throw exceptions.</remarks>
        public event EventHandler<EventArgs<IList<K>>> Terminated;

        /// <summary>
        /// Adds new object to life-cycle management and tracking.
        /// </summary>
        /// <param name="key">Key by which object can be identified.</param>
        /// <param name="obj">Object instance to track.</param>
        public void Add(K key, O obj) {
            CheckDisposed();
            if (obj == null)
                throw new ArgumentNullException("obj");
            objectMap.Add(key, obj);
        }

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
            var result = !objectMap.ContainsKey(key);
            if (result)
                objectMap[key] = obj;
            return result;
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
            O oldObj;
            if (objectMap.TryGetValue(key, out oldObj)) {
                var lc = oldObj.GetLifeCycle();
                lc.Terminate();
            }
            objectMap[key] = obj;
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
            O oldObj;
            if (objectMap.TryGetValue(key, out oldObj)) {
                var lc = oldObj.GetLifeCycle();
                if (lc.CheckAlive()) {
                    lc.Used();
                    return oldObj;
                }
                lc.Terminate();
            }
            objectMap[key] = obj;
            return obj;
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
            O obj;
            if (objectMap.TryGetValue(key, out obj)) {
                var lc = obj.GetLifeCycle();
                if (lc.CheckAlive()) {
                    lc.Used();
                    return obj;
                }
                lc.Terminate();
            }
            obj = getNew(key);
            objectMap[key] = obj;
            return obj;
        }

        /// <summary>
        /// Returns "live" object from object map.
        /// </summary>
        /// <param name="key">Key by which object can be identified.</param>
        /// <param name="obj">Object to return.</param>
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
                objectMap.Remove(key);
            }
            return false;
        }

        public bool Remove(K key) {
            CheckDisposed();
            return objectMap.Remove(key);
        }

        /// <summary>
        /// Terminates life cycle for a given object and removes it from object map immediately.
        /// </summary>
        /// <param name="key">Key by which object can be identified.</param>
        /// <returns><c>true</c> if object was found and terminated, <c>false</c> otherwise.</returns>
        public bool Terminate(K key) {
            CheckDisposed();
            O obj;
            if (objectMap.TryGetValue(key, out obj)) {
                var lc = obj.GetLifeCycle();
                lc.Terminate();
                return objectMap.Remove(key);
            }
            return false;
        }

        /// <summary>
        /// Terminates all object lifecycles managed by this instance.
        /// </summary>
        public void TerminateAll() {
            CheckDisposed();
            foreach (var entry in objectMap) {
                var lc = entry.Value.GetLifeCycle();
                lc.Terminate();
            }
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
