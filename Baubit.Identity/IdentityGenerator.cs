using System;

namespace Baubit.Identity
{
    /// <summary>
    /// Provides a monotonic identity generator that wraps <see cref="GuidV7Generator"/>.
    /// Implements <see cref="IIdentityGenerator"/> for abstraction and testability.
    /// </summary>
    public sealed class IdentityGenerator : IIdentityGenerator
    {
        private readonly GuidV7Generator _generator;

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityGenerator"/> class.
        /// </summary>
        /// <param name="generator">The underlying <see cref="GuidV7Generator"/> instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when generator is null.</exception>
        private IdentityGenerator(GuidV7Generator generator)
        {
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        }

        /// <summary>
        /// Creates a new identity generator with default settings.
        /// </summary>
        /// <param name="maxDriftMs">Maximum allowed drift ahead of wall-clock in milliseconds. Null means no cap.</param>
        /// <param name="throwOnDriftCap">If true and drift cap is exceeded, throws exception instead of clamping.</param>
        /// <returns>A new <see cref="IdentityGenerator"/> instance.</returns>
        public static IdentityGenerator CreateNew(long? maxDriftMs = null, bool throwOnDriftCap = false)
        {
            var generator = GuidV7Generator.CreateNew(maxDriftMs, throwOnDriftCap);
            return new IdentityGenerator(generator);
        }

        /// <summary>
        /// Creates a new identity generator seeded from an existing version 7 GUID.
        /// </summary>
        /// <param name="existingV7">The version 7 GUID to seed from.</param>
        /// <param name="maxDriftMs">Maximum allowed drift ahead of wall-clock in milliseconds. Null means no cap.</param>
        /// <param name="throwOnDriftCap">If true and drift cap is exceeded, throws exception instead of clamping.</param>
        /// <returns>A new <see cref="IdentityGenerator"/> instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the provided GUID is not version 7.</exception>
        public static IdentityGenerator CreateNew(Guid existingV7, long? maxDriftMs = null, bool throwOnDriftCap = false)
        {
            var generator = GuidV7Generator.CreateNew(existingV7, maxDriftMs, throwOnDriftCap);
            return new IdentityGenerator(generator);
        }

        /// <summary>
        /// Seed the generator from an existing version 7 GUID to ensure future IDs never go backwards.
        /// </summary>
        /// <param name="existingV7">The version 7 GUID to seed from.</param>
        /// <exception cref="InvalidOperationException">Thrown when the provided GUID is not version 7.</exception>
        public void InitializeFrom(Guid existingV7)
        {
            _generator.InitializeFrom(existingV7);
        }

        /// <summary>
        /// Seed the generator from a specific UTC timestamp to ensure future IDs never go backwards.
        /// </summary>
        /// <param name="timestampUtc">The UTC timestamp to seed from.</param>
        public void InitializeFrom(DateTimeOffset timestampUtc)
        {
            _generator.InitializeFrom(timestampUtc);
        }

        /// <summary>
        /// Generate a strictly increasing identity value using the current UTC time.
        /// </summary>
        /// <returns>A new monotonic GUID.</returns>
        public Guid GetNext()
        {
            return _generator.GetNext();
        }

        /// <summary>
        /// Generate a strictly increasing identity value using a specific UTC timestamp.
        /// </summary>
        /// <param name="timestampUtc">The UTC timestamp to use for generation.</param>
        /// <returns>A new monotonic GUID.</returns>
        public Guid GetNext(DateTimeOffset timestampUtc)
        {
            return _generator.GetNext(timestampUtc);
        }
    }
}
