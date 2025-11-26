using System.Collections.Concurrent;

namespace Baubit.Identity.Test.GuidV7
{
    public class Test
    {
        [Fact]
        public void CreateVersion7_HasCorrectVersionBits()
        {
            var guid = Identity.GuidV7.CreateVersion7();
            byte[] bytes = guid.ToByteArray();

            // Version bits are in byte 7 (high nibble) in ToByteArray() output
            int version = (bytes[7] >> 4) & 0x0F;
            Assert.Equal(7, version);
        }

        [Fact]
        public void CreateVersion7_HasCorrectVariantBits()
        {
            var guid = Identity.GuidV7.CreateVersion7();
            byte[] bytes = guid.ToByteArray();

            // Variant bits are in byte 8 (high 2 bits should be 10)
            int variant = (bytes[8] >> 6) & 0x03;
            Assert.Equal(2, variant); // 10 in binary = 2
        }

        [Fact]
        public void CreateVersion7_WithTimestamp_EncodesCorrectTimestamp()
        {
            var timestamp = DateTimeOffset.UtcNow;
            var expectedMs = timestamp.ToUnixTimeMilliseconds();

            var guid = Identity.GuidV7.CreateVersion7(timestamp);

            Assert.True(Identity.GuidV7.TryGetUnixMs(guid, out long extractedMs));
            Assert.Equal(expectedMs, extractedMs);
        }

        [Fact]
        public void IsVersion7_ReturnsTrueForVersion7Guid()
        {
            var guid = Identity.GuidV7.CreateVersion7();
            Assert.True(Identity.GuidV7.IsVersion7(guid));
        }

        [Fact]
        public void IsVersion7_ReturnsFalseForRandomGuid()
        {
            var guid = Guid.NewGuid(); // This creates a version 4 GUID
            Assert.False(Identity.GuidV7.IsVersion7(guid));
        }

        [Fact]
        public void TryGetUnixMs_ReturnsFalseForNonVersion7Guid()
        {
            var guid = Guid.NewGuid();
            Assert.False(Identity.GuidV7.TryGetUnixMs(guid, out long ms));
            Assert.Equal(0, ms);
        }

        [Fact]
        public void TryGetUnixMs_ExtractsCorrectTimestamp()
        {
            var now = DateTimeOffset.UtcNow;
            var expectedMs = now.ToUnixTimeMilliseconds();

            var guid = Identity.GuidV7.CreateVersion7(now);

            Assert.True(Identity.GuidV7.TryGetUnixMs(guid, out long extractedMs));
            Assert.Equal(expectedMs, extractedMs);
        }

        [Fact]
        public void CreateVersion7_GeneratesUniqueGuids()
        {
            const int count = 10000;
            var guids = new HashSet<Guid>();

            for (int i = 0; i < count; i++)
            {
                var guid = Identity.GuidV7.CreateVersion7();
                Assert.True(guids.Add(guid), $"Duplicate GUID generated at iteration {i}");
            }

            Assert.Equal(count, guids.Count);
        }

        [Fact]
        public void CreateVersion7_ThreadSafety()
        {
            const int count = 10000;
            var guids = new ConcurrentBag<Guid>();

            Parallel.For(0, count, _ =>
            {
                guids.Add(Identity.GuidV7.CreateVersion7());
            });

            Assert.Equal(count, guids.Count);
            Assert.Equal(count, guids.Distinct().Count());
        }

        [Fact]
        public void CreateVersion7_EpochTime_EncodesCorrectly()
        {
            var epoch = DateTimeOffset.UnixEpoch;
            var guid = Identity.GuidV7.CreateVersion7(epoch);

            Assert.True(Identity.GuidV7.TryGetUnixMs(guid, out long ms));
            Assert.Equal(0, ms);
        }

        [Fact]
        public void CreateVersion7_FarFutureTime_EncodesCorrectly()
        {
            // Test a far future timestamp (year 2100)
            var futureTime = new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var expectedMs = futureTime.ToUnixTimeMilliseconds();

            var guid = Identity.GuidV7.CreateVersion7(futureTime);

            Assert.True(Identity.GuidV7.TryGetUnixMs(guid, out long ms));
            Assert.Equal(expectedMs, ms);
        }

        [Fact]
        public void CreateVersion7_MillisecondBoundary_EncodesCorrectly()
        {
            // Test at exact millisecond boundaries
            var baseTime = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

            for (int i = 0; i < 1000; i++)
            {
                var time = baseTime.AddMilliseconds(i);
                var expectedMs = time.ToUnixTimeMilliseconds();

                var guid = Identity.GuidV7.CreateVersion7(time);

                Assert.True(Identity.GuidV7.TryGetUnixMs(guid, out long ms));
                Assert.Equal(expectedMs, ms);
            }
        }

        [Fact]
        public void CreateVersion7_MonotonicTimestamps_ProduceOrderedGuids()
        {
            var guids = new List<Guid>();
            var baseTime = DateTimeOffset.UtcNow;

            // Generate GUIDs with increasing timestamps
            for (int i = 0; i < 100; i++)
            {
                var time = baseTime.AddMilliseconds(i);
                guids.Add(Identity.GuidV7.CreateVersion7(time));
            }

            // Verify timestamps are in order
            for (int i = 1; i < guids.Count; i++)
            {
                Identity.GuidV7.TryGetUnixMs(guids[i - 1], out long prevMs);
                Identity.GuidV7.TryGetUnixMs(guids[i], out long currMs);

                Assert.True(currMs >= prevMs, $"GUID at index {i} has earlier timestamp than index {i - 1}");
            }
        }

        [Fact]
        public void CreateVersion7_RFC9562Compliance_ByteStructure()
        {
            var timestamp = new DateTimeOffset(2024, 1, 15, 12, 30, 45, 123, TimeSpan.Zero);
            var expectedMs = timestamp.ToUnixTimeMilliseconds();

            var guid = Identity.GuidV7.CreateVersion7(timestamp);
            byte[] bytes = guid.ToByteArray();

            // Verify version (byte 7, high nibble)
            Assert.Equal(7, (bytes[7] >> 4) & 0x0F);

            // Verify variant (byte 8, high 2 bits = 10)
            Assert.Equal(0x80, bytes[8] & 0xC0);

            // Verify timestamp extraction matches
            Assert.True(Identity.GuidV7.TryGetUnixMs(guid, out long extractedMs));
            Assert.Equal(expectedMs, extractedMs);
        }

        [Fact]
        public void CreateVersion7_AllGuidsAreVersion7()
        {
            for (int i = 0; i < 1000; i++)
            {
                var guid = Identity.GuidV7.CreateVersion7();
                Assert.True(Identity.GuidV7.IsVersion7(guid), $"GUID at iteration {i} is not version 7");
            }
        }

        [Theory]
        [InlineData(0)] // Epoch
        [InlineData(1000)] // 1 second after epoch
        [InlineData(1609459200000)] // 2021-01-01 00:00:00 UTC
        [InlineData(1735689600000)] // 2025-01-01 00:00:00 UTC
        public void CreateVersion7_VariousTimestamps_RoundTrip(long unixMs)
        {
            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(unixMs);
            var guid = Identity.GuidV7.CreateVersion7(timestamp);

            Assert.True(Identity.GuidV7.TryGetUnixMs(guid, out long extractedMs));
            Assert.Equal(unixMs, extractedMs);
        }

        [Fact]
        public void TryGetUnixMs_EmptyGuid_ReturnsFalse()
        {
            var emptyGuid = Guid.Empty;
            Assert.False(Identity.GuidV7.TryGetUnixMs(emptyGuid, out long ms));
            Assert.Equal(0, ms);
        }

        [Fact]
        public void CreateVersion7_WithSameTimestamp_GeneratesDifferentRandomBits()
        {
            var timestamp = DateTimeOffset.UtcNow;
            var guid1 = Identity.GuidV7.CreateVersion7(timestamp);
            var guid2 = Identity.GuidV7.CreateVersion7(timestamp);

            // Both should have the same timestamp
            Identity.GuidV7.TryGetUnixMs(guid1, out long ms1);
            Identity.GuidV7.TryGetUnixMs(guid2, out long ms2);
            Assert.Equal(ms1, ms2);

            // But they should be different GUIDs (different random parts)
            Assert.NotEqual(guid1, guid2);
        }

        [Fact]
        public void CreateVersion7_GeneratorCompatibility()
        {
            // Test that GUIDs from GuidV7.CreateVersion7 work with GuidV7Generator
            var timestamp = DateTimeOffset.UtcNow;
            var seedGuid = Identity.GuidV7.CreateVersion7(timestamp);

            var generator = Identity.GuidV7Generator.CreateNew(seedGuid);
            Assert.NotNull(generator);
            Assert.Equal(timestamp.ToUnixTimeMilliseconds(), generator.LastIssuedUnixMs);

            // Generator should be able to extract timestamp from its own GUIDs
            var generatedGuid = generator.GetNext();
            Assert.True(Identity.GuidV7.IsVersion7(generatedGuid));
            Assert.True(Identity.GuidV7.TryGetUnixMs(generatedGuid, out _));
        }
    }
}
