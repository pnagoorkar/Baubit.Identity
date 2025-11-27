using System;
using System.Security.Cryptography;

namespace Baubit.Identity
{
    /// <summary>
    /// Provides static methods for creating and validating RFC 9562 compliant UUIDv7 identifiers.
    /// This class is compatible with .NET Standard 2.0.
    /// </summary>
    public static class GuidV7
    {
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

            // RFC 9562 UUIDv7 structure (128 bits):
            // 0-47:   48-bit Unix timestamp in milliseconds (big-endian in wire format)
            // 48-51:  4-bit version (0111 = 7)
            // 52-63:  12-bit random
            // 64-65:  2-bit variant (10)
            // 66-127: 62-bit random

            // We need 10 random bytes: 12 bits (2 bytes, but only 12 bits used) + 62 bits (8 bytes, but only 62 bits used)
            // Actually: 74 bits of random = 10 bytes (80 bits), mask off what we need
            byte[] randomBytes = new byte[10];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            // Build the 16-byte array in RFC 4122 big-endian wire format
            byte[] bytes = new byte[16];

            // Bytes 0-5: 48-bit Unix timestamp (big-endian)
            bytes[0] = (byte)((unixMs >> 40) & 0xFF);
            bytes[1] = (byte)((unixMs >> 32) & 0xFF);
            bytes[2] = (byte)((unixMs >> 24) & 0xFF);
            bytes[3] = (byte)((unixMs >> 16) & 0xFF);
            bytes[4] = (byte)((unixMs >> 8) & 0xFF);
            bytes[5] = (byte)(unixMs & 0xFF);

            // Byte 6: version (7) in high nibble + 4 bits of random in low nibble
            bytes[6] = (byte)(0x70 | (randomBytes[0] & 0x0F));

            // Byte 7: 8 bits of random
            bytes[7] = randomBytes[1];

            // Byte 8: variant (10) in high 2 bits + 6 bits of random
            bytes[8] = (byte)(0x80 | (randomBytes[2] & 0x3F));

            // Bytes 9-15: 56 bits of random
            bytes[9] = randomBytes[3];
            bytes[10] = randomBytes[4];
            bytes[11] = randomBytes[5];
            bytes[12] = randomBytes[6];
            bytes[13] = randomBytes[7];
            bytes[14] = randomBytes[8];
            bytes[15] = randomBytes[9];

            // Convert from RFC 4122 big-endian wire format to .NET Guid format
            // .NET Guid constructor expects: a (int), b (short), c (short), d-k (8 bytes)
            // The first 8 bytes (a, b, c) are stored in little-endian on little-endian systems
            // We need to convert from big-endian wire format to the expected format
            return new Guid(
                // a (bytes 0-3, big-endian in wire) -> int (stored as little-endian by .NET on LE systems)
                (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3],
                // b (bytes 4-5, big-endian in wire) -> short
                (short)((bytes[4] << 8) | bytes[5]),
                // c (bytes 6-7, big-endian in wire) -> short
                (short)((bytes[6] << 8) | bytes[7]),
                // d-k (bytes 8-15) are stored as-is
                bytes[8], bytes[9], bytes[10], bytes[11],
                bytes[12], bytes[13], bytes[14], bytes[15]
            );
        }

        /// <summary>
        /// Determines whether the specified GUID is a version 7 UUID.
        /// </summary>
        /// <param name="guid">The GUID to check.</param>
        /// <returns>true if the GUID is version 7; otherwise, false.</returns>
        public static bool IsVersion7(Guid guid)
        {
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
        }

        /// <summary>
        /// Attempts to extract the Unix milliseconds timestamp from a UUIDv7.
        /// </summary>
        /// <param name="guid">The GUID to extract the timestamp from.</param>
        /// <param name="ms">When this method returns, contains the Unix milliseconds timestamp if the GUID is version 7.</param>
        /// <returns>true if the GUID is version 7 and the timestamp was extracted; otherwise, false.</returns>
        public static bool TryGetUnixMs(Guid guid, out long ms)
        {
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

            // Convert from .NET format back to wire format for timestamp extraction
            byte[] wireBytes = new byte[6];
            // 'a' is bytes[0-3] in little-endian, wire format is big-endian
            wireBytes[0] = bytes[3];
            wireBytes[1] = bytes[2];
            wireBytes[2] = bytes[1];
            wireBytes[3] = bytes[0];
            // 'b' is bytes[4-5] in little-endian, wire format is big-endian
            wireBytes[4] = bytes[5];
            wireBytes[5] = bytes[4];

            // Now wireBytes[0-5] contains the 48-bit timestamp in big-endian
            ulong timestamp =
                ((ulong)wireBytes[0] << 40) |
                ((ulong)wireBytes[1] << 32) |
                ((ulong)wireBytes[2] << 24) |
                ((ulong)wireBytes[3] << 16) |
                ((ulong)wireBytes[4] << 8) |
                (ulong)wireBytes[5];

            ms = (long)timestamp;
            return true;
        }
    }
}
