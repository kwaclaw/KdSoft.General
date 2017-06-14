using System;

namespace KdSoft
{
    /// <summary>
    /// Exposes a property to be used for identity purposes.
    /// </summary>
    /// <typeparam name="TKey">Type of Id property.</typeparam>
    public interface IIdentifiable<TKey> where TKey : IEquatable<TKey>
    {
        /// <summary>Identifier.</summary>
        TKey Id { get; }
    }
}
