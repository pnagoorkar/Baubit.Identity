using System;
using System.Security.Cryptography;
using System.Threading;

namespace Baubit.Identity
{
    /// <summary>
    /// Provides static methods for creating and validating RFC 9562 compliant UUIDv7 identifiers.
    /// This class is compatible with .NET Standard 2.0 and optimized for .NET 9.0.
    /// </summary>
    public static class GuidV7
    {
#if NETSTANDARD2_0
        // Thread-local RandomNumberGenerator for .NET Standard 2.0.
        // Not disposed intentionally - these are static resources meant to live for application lifetime.
        // The factory function always returns a valid instance.
        private static readonly ThreadLocal<RandomNumberGenerator> s_rng = 
            new ThreadLocal<RandomNumberGenerator>(() => RandomNumberGenerator.Create(), trackAllValues: false);

        // Thread-local byte array for random bytes in .NET Standard 2.0
        private static readonly ThreadLocal<byte[]> s_randomBuffer = 
            new ThreadLocal<byte[]>(() => new byte[10], trackAllValues: false);
#endif

        /// <summary>
        /// Creates an RFC 9562 compliant UUIDv7 using the current UTC time.
        /// </summary>
        /// <returns>A new UUIDv7 GUID.</returns>
        public static Guid CreateVersion7()
        {
            return CreateVersion7(DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// Creates an RFC 9562 compliant UUIDv7 using the specified timestamp.
        /// </summary>
        /// <param name="timestamp">The timestamp to encode in the GUID.</param>
        /// <returns>A new UUIDv7 GUID with the specified timestamp.</returns>
        public static Guid CreateVersion7(DateTimeOffset timestamp)
        {
            long unixMs = timestamp.ToUnixTimeMilliseconds();
            return CreateVersion7Core(unixMs);
        }

        /// <summary>
        /// Creates an RFC 9562 compliant UUIDv7 from a Unix timestamp in milliseconds.
        /// </summary>
        internal static Guid CreateVersion7Core(long unixMs)
        {
            // RFC 9562 UUIDv7 structure (128 bits):
            // 0-47:   48-bit Unix timestamp in milliseconds (big-endian in wire format)
            // 48-51:  4-bit version (0111 = 7)
            // 52-63:  12-bit random
            // 64-65:  2-bit variant (10)
            // 66-127: 62-bit random

#if NET9_0_OR_GREATER
            // Use Random.Shared for fast, thread-safe random generation on .NET 9+
            // RFC 9562 does not require cryptographically secure random numbers for UUIDv7
            Span<byte> randomBytes = stackalloc byte[10];
            Random.Shared.NextBytes(randomBytes);

            // Build GUID directly from random bytes and timestamp
            // d-k (bytes 8-15) with variant bits set
            byte d = (byte)(0x80 | (randomBytes[2] & 0x3F)); // variant (10) + 6 random bits
            byte e = randomBytes[3];
            byte f = randomBytes[4];
            byte g = randomBytes[5];
            byte h = randomBytes[6];
            byte i = randomBytes[7];
            byte j = randomBytes[8];
            byte k = randomBytes[9];

            // Construct 'a' component from timestamp bytes 0-3 (big-endian in wire, needs conversion)
            int a = (int)(((unixMs >> 40) & 0xFF) << 24) |
                    (int)(((unixMs >> 32) & 0xFF) << 16) |
                    (int)(((unixMs >> 24) & 0xFF) << 8) |
                    (int)((unixMs >> 16) & 0xFF);

            // Construct 'b' component from timestamp bytes 4-5
            short b = (short)(((unixMs >> 8) & 0xFF) << 8 | (unixMs & 0xFF));

            // Construct 'c' component: version (7) in high nibble + random
            short c = (short)((0x70 | (randomBytes[0] & 0x0F)) << 8 | randomBytes[1]);

            return new Guid(a, b, c, d, e, f, g, h, i, j, k);
#else
            // .NET Standard 2.0: Use thread-local buffers to reduce allocations
            // The factory functions guarantee non-null values, but we add defensive checks
            var rng = s_rng.Value ?? throw new InvalidOperationException("RandomNumberGenerator not initialized");
            var randomBytes = s_randomBuffer.Value ?? throw new InvalidOperationException("Random buffer not initialized");
            rng.GetBytes(randomBytes);

            // d-k (bytes 8-15) with variant bits set
            byte d = (byte)(0x80 | (randomBytes[2] & 0x3F)); // variant (10) + 6 random bits
            byte e = randomBytes[3];
            byte f = randomBytes[4];
            byte g = randomBytes[5];
            byte h = randomBytes[6];
            byte i = randomBytes[7];
            byte j = randomBytes[8];
            byte k = randomBytes[9];

            // Construct 'a' component from timestamp bytes 0-3 (big-endian in wire, needs conversion)
            int a = (int)(((unixMs >> 40) & 0xFF) << 24) |
                    (int)(((unixMs >> 32) & 0xFF) << 16) |
                    (int)(((unixMs >> 24) & 0xFF) << 8) |
                    (int)((unixMs >> 16) & 0xFF);

            // Construct 'b' component from timestamp bytes 4-5
            short b = (short)(((unixMs >> 8) & 0xFF) << 8 | (unixMs & 0xFF));

            // Construct 'c' component: version (7) in high nibble + random
            short c = (short)((0x70 | (randomBytes[0] & 0x0F)) << 8 | randomBytes[1]);

            return new Guid(a, b, c, d, e, f, g, h, i, j, k);
#endif
        }

        /// <summary>
        /// Determines whether the specified GUID is a version 7 UUID.
        /// </summary>
        /// <param name="guid">The GUID to check.</param>
        /// <returns>true if the GUID is version 7; otherwise, false.</returns>
        public static bool IsVersion7(Guid guid)
        {
#if NET9_0_OR_GREATER
            // Use TryWriteBytes to avoid allocation on .NET 9+
            Span<byte> bytes = stackalloc byte[16];
            guid.TryWriteBytes(bytes);
            int version = (bytes[7] >> 4) & 0x0F;
            return version == 7;
#else
            // The version is stored in the high nibble of byte 6 (in wire format)
            // In .NET Guid, we can extract this from the ToByteArray() output
            byte[] bytes = guid.ToByteArray();

            // .NET Guid byte layout (platform-independent, defined by RFC 4122):
            // bytes[0-3]: 'a' component (stored as little-endian in memory)
            // bytes[4-5]: 'b' component (stored as little-endian in memory)
            // bytes[6-7]: 'c' component (stored as little-endian in memory)
            // bytes[8-15]: d through k (stored as-is, big-endian)
            //
            // The version bits are in the high nibble of byte 7 in ToByteArray() output
            // because 'c' is stored little-endian, so the high byte of 'c' is at bytes[7]
            int version = (bytes[7] >> 4) & 0x0F;
            return version == 7;
#endif
        }

        /// <summary>
        /// Attempts to extract the Unix milliseconds timestamp from a UUIDv7.
        /// </summary>
        /// <param name="guid">The GUID to extract the timestamp from.</param>
        /// <param name="ms">When this method returns, contains the Unix milliseconds timestamp if the GUID is version 7.</param>
        /// <returns>true if the GUID is version 7 and the timestamp was extracted; otherwise, false.</returns>
        public static bool TryGetUnixMs(Guid guid, out long ms)
        {
#if NET9_0_OR_GREATER
            // Use TryWriteBytes to avoid allocation on .NET 9+
            Span<byte> bytes = stackalloc byte[16];
            guid.TryWriteBytes(bytes);

            // Check version
            int version = (bytes[7] >> 4) & 0x0F;
            if (version != 7)
            {
                ms = 0;
                return false;
            }

            // Extract timestamp from .NET Guid byte layout
            // 'a' is bytes[0-3] in little-endian, wire format is big-endian
            // 'b' is bytes[4-5] in little-endian, wire format is big-endian
            ulong timestamp =
                ((ulong)bytes[3] << 40) |
                ((ulong)bytes[2] << 32) |
                ((ulong)bytes[1] << 24) |
                ((ulong)bytes[0] << 16) |
                ((ulong)bytes[5] << 8) |
                (ulong)bytes[4];

            ms = (long)timestamp;
            return true;
#else
            if (!IsVersion7(guid))
            {
                ms = 0;
                return false;
            }

            byte[] bytes = guid.ToByteArray();

            // .NET Guid byte layout (platform-independent, defined by RFC 4122):
            // bytes[0-3]: 'a' component (stored as little-endian in memory)
            // bytes[4-5]: 'b' component (stored as little-endian in memory)
            // bytes[6-7]: 'c' component (stored as little-endian in memory)
            // bytes[8-15]: d through k (stored as-is, big-endian)
            //
            // To get the 48-bit timestamp, we reverse the byte order for a and b components
            // to reconstruct the RFC 9562 wire format bytes 0-5

            // Extract timestamp directly without intermediate array
            ulong timestamp =
                ((ulong)bytes[3] << 40) |
                ((ulong)bytes[2] << 32) |
                ((ulong)bytes[1] << 24) |
                ((ulong)bytes[0] << 16) |
                ((ulong)bytes[5] << 8) |
                (ulong)bytes[4];

            ms = (long)timestamp;
            return true;
#endif
        }
    }
}
