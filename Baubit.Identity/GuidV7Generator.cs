using System;
using System.Threading;

namespace Baubit.Identity
{
    public sealed class GuidV7Generator
    {
        // Last emitted Unix timestamp (milliseconds). -1 means "not set".
        private long _lastMs = -1;

        /// <summary>Maximum allowed drift ahead of wall-clock (ms). Null = no cap.</summary>
        public long? MaxDriftMs { get; set; }

        /// <summary>If true and drift cap is exceeded, throw instead of clamping.</summary>
        public bool ThrowOnDriftCap { get; set; }

        private GuidV7Generator(long? maxDriftMs = null, bool throwOnDriftCap = false)
        {
            MaxDriftMs = maxDriftMs;
            ThrowOnDriftCap = throwOnDriftCap;
        }

        public static GuidV7Generator CreateNew(long? maxDriftMs = null, bool throwOnDriftCap = false)
        {
            return CreateNew(GuidV7.CreateVersion7(), maxDriftMs, throwOnDriftCap);
        }

        /// <summary>Create a generator seeded from the timestamp in an existing UUIDv7.</summary>
        public static GuidV7Generator CreateNew(Guid existingV7, long? maxDriftMs = null, bool throwOnDriftCap = false)
        {
            if (!GuidV7.TryGetUnixMs(existingV7, out long ms))
                throw new InvalidOperationException("CreateNew(Guid) requires a version 7 GUID.");

            var gen = new GuidV7Generator(maxDriftMs, throwOnDriftCap);
            gen.Seed(ms);
            return gen;
        }

        /// <summary>Seed the generator so future IDs never go backwards.</summary>
        public void InitializeFrom(Guid existingV7)
        {
            if (!GuidV7.TryGetUnixMs(existingV7, out long ms))
                throw new InvalidOperationException("InitializeFrom(Guid) requires a version 7 GUID.");
            Seed(ms);
        }

        /// <summary>Seed from a specific UTC timestamp.</summary>
        public void InitializeFrom(DateTimeOffset timestampUtc) => Seed(timestampUtc.ToUnixTimeMilliseconds());

        /// <summary>Generate a strictly increasing UUIDv7 (Guid) using current UTC time.</summary>
        public Guid GetNext() => GetNext(DateTimeOffset.UtcNow);

        /// <summary>Generate using a specific UTC timestamp (useful for tests/replay).</summary>
        public Guid GetNext(DateTimeOffset timestampUtc) => NewInternal(timestampUtc.ToUnixTimeMilliseconds());

        /// <summary>For diagnostics/tests only: last issued ms (or -1 if none).</summary>
        public long LastIssuedUnixMs => Volatile.Read(ref _lastMs);

        private void Seed(long ms)
        {
            long curr = Volatile.Read(ref _lastMs);
            if (ms > curr) Volatile.Write(ref _lastMs, ms);
        }

        private Guid NewInternal(long nowMs)
        {
            long next;
            while (true)
            {
                long last = Volatile.Read(ref _lastMs);
                next = (nowMs > last) ? nowMs : last + 1;

                // Optional max-drift guard
                if (MaxDriftMs is long cap && next - nowMs > cap)
                {
                    if (ThrowOnDriftCap)
                        throw new InvalidOperationException(
                            $"Monotonic v7 drift cap exceeded: next={next}, now={nowMs}, cap={cap}ms.");
                    next = nowMs + cap;
                }

                if (Interlocked.CompareExchange(ref _lastMs, next, last) == last)
                    break; // reserved "next"
            }

            return GuidV7.CreateVersion7(DateTimeOffset.FromUnixTimeMilliseconds(next));
        }

        /// <summary>Extract the Unix milliseconds (48-bit) from a UUIDv7.</summary>
        public static bool TryGetUnixMs(Guid guid, out long ms)
        {
            return GuidV7.TryGetUnixMs(guid, out ms);
        }
    }

    public static class GuidV7Extensions
    {
        public static long? ExtractTimestampMs(this Guid guid)
        {
            if (GuidV7.TryGetUnixMs(guid, out var ms))
            {
                return ms;
            }
            return null;
        }
    }
}
