using System;

namespace KdSoft.Utils
{
    /// <summary>
    /// Interface for managing object life cycle
    /// </summary>
    public interface ILifeCycle
    {
        /// <summary>
        /// Checks if object's life-cycle is still active, and initiates object disposal if life-cycle has ended.
        /// Ongoing operations (calls in progress) will still be finished.
        /// </summary>
        /// <returns><c>true</c> if life-cycle is active, <c>false</c> otherwise.</returns>
        /// <remarks>Must not throw an exception. Should have very short duration (possibly starting async background operation for disposal).</remarks>
        bool CheckAlive();

        /// <summary>
        /// Terminates life-cycle, even if its natural end condition has not been reached yet.
        /// Ongoing operations (calls in progress) will still be finished.
        /// </summary>
        /// <remarks>Must not throw an exception.</remarks>
        void Terminate();
    }

    /// <summary>
    /// Interface that a life-cycle aware object must implement.
    /// </summary>
    public interface ILifeCycleAware<T> where T : ILifeCycle
    {
        /// <summary>
        /// Returns the <see cref="ILifeCycle"/> derived interface for the object.
        /// </summary>
        T GetLifeCycle();
    }

    /// <summary>
    /// Interface for objects that have a "non-activity time-out" based life-cycle.
    /// </summary>
    public interface ITimedLifeCycle: ILifeCycle
    {
        /// <summary>
        /// Timespan of non-activity after which object should be destroyed.
        /// </summary>
        TimeSpan LifeSpan { get; }

        /// <summary>
        /// Must be called whenever the object is used/activated to "renew its lease on life".
        /// </summary>
        /// <returns>The time of use, or <c>null</c> when object has already expired.</returns>
        DateTimeOffset? Used();

        /// <summary>
        /// Returns the time of last use.
        /// </summary>
        DateTimeOffset LastUsed { get; }
    }

    /// <summary>
    /// Interface for objects that are reference-counted.
    /// </summary>
    public interface IRefCountLifeCycle: ILifeCycle
    {
        /// <summary>
        /// Increments reference count.
        /// </summary>
        /// <returns>New reference count.</returns>
        int IncRefCount();

        /// <summary>
        /// Decrements reference count.
        /// </summary>
        /// <returns>New reference count.</returns>
        int DecRefCount();
    }
}
