using System;

namespace Baubit.Identity
{
    /// <summary>
    /// Defines a contract for generating monotonic identity values.
    /// </summary>
    public interface IIdentityGenerator
    {
        /// <summary>
        /// Seed the generator from an existing version 7 GUID to ensure future IDs never go backwards.
        /// </summary>
        /// <param name="existingV7">The version 7 GUID to seed from.</param>
        /// <exception cref="InvalidOperationException">Thrown when the provided GUID is not version 7.</exception>
        void InitializeFrom(Guid existingV7);

        /// <summary>
        /// Seed the generator from a specific UTC timestamp to ensure future IDs never go backwards.
        /// </summary>
        /// <param name="timestampUtc">The UTC timestamp to seed from.</param>
        void InitializeFrom(DateTimeOffset timestampUtc);

        /// <summary>
        /// Generate a strictly increasing identity value using the current UTC time.
        /// </summary>
        /// <returns>A new monotonic GUID.</returns>
        Guid GetNext();

        /// <summary>
        /// Generate a strictly increasing identity value using a specific UTC timestamp.
        /// </summary>
        /// <param name="timestampUtc">The UTC timestamp to use for generation.</param>
        /// <returns>A new monotonic GUID.</returns>
        Guid GetNext(DateTimeOffset timestampUtc);
    }
}
