using System.Collections.Concurrent;

namespace Baubit.Identity.Test.GuidV7Generator
{
    public class Test
    {
        [Fact]
        public void CanCreateDefault()
        {
            var guidV7Generator = Baubit.Identity.GuidV7Generator.CreateNew();
            Assert.NotNull(guidV7Generator);
        }

        [Fact]
        public void CanCreateUsingGuidV7()
        {
            var dateTimeOffset = DateTimeOffset.UtcNow;
            var reference = Identity.GuidV7.CreateVersion7(dateTimeOffset);
            var guidV7Generator = Baubit.Identity.GuidV7Generator.CreateNew(reference);
            Assert.NotNull(guidV7Generator);
            Assert.Equal(dateTimeOffset.ToUnixTimeMilliseconds(), guidV7Generator.LastIssuedUnixMs);
        }

        [Fact]
        public void CanNotCreateWithoutGuidV7()
        {
            Assert.Throws<InvalidOperationException>(() => Baubit.Identity.GuidV7Generator.CreateNew(Guid.NewGuid()));
        }

        [Fact]
        public void CanProgressSeedUsingDateTimeOffset()
        {
            var guidV7Generator = Baubit.Identity.GuidV7Generator.CreateNew();
            var guid1 = guidV7Generator.GetNext();
            var lastMs1 = guidV7Generator.LastIssuedUnixMs;
            var dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(lastMs1 + 1);
            var guid2 = guidV7Generator.GetNext(dateTimeOffset);
            var lastMs2 = guidV7Generator.LastIssuedUnixMs;

            Assert.True(lastMs1 + 1 == lastMs2);
        }

        [Fact]
        public void CanProgressSeedUsingGuidV7()
        {
            var initTime = DateTimeOffset.UtcNow;
            var initTimestamp = initTime.ToUnixTimeMilliseconds();
            var initGuid = Identity.GuidV7.CreateVersion7(initTime);
            var generator1 = Baubit.Identity.GuidV7Generator.CreateNew(initGuid);
            var generator2 = Baubit.Identity.GuidV7Generator.CreateNew(initGuid);

            // generators created with the same seed as initTime
            Assert.Equal(generator1.LastIssuedUnixMs, generator2.LastIssuedUnixMs);
            Assert.Equal(initTimestamp, generator1.LastIssuedUnixMs);
            Assert.Equal(initTimestamp, generator2.LastIssuedUnixMs);

            var driftTime = initTime.AddHours(1);

            var driftGuid = Identity.GuidV7.CreateVersion7(driftTime);

            generator2.InitializeFrom(driftGuid); // tell the generator we want generated ids to have a timestamp AFTER the drifted time
            var driftTimestamp = driftTime.ToUnixTimeMilliseconds();

            Assert.Equal(driftTimestamp, generator2.LastIssuedUnixMs); // generator2 seed reflects the drifted time

            var guid1 = generator1.GetNext(initTime);
            var guid2 = generator2.GetNext(initTime);

            Baubit.Identity.GuidV7Generator.TryGetUnixMs(guid1, out var guid1Timestamp);
            Baubit.Identity.GuidV7Generator.TryGetUnixMs(guid2, out var guid2Timestamp);

            // undrifted generator generates a guid with a timestamp 1 ms after the init time because the last issued guid was at initTimestamp
            Assert.Equal(guid1Timestamp, initTimestamp + 1);
            // drifted generator generates a guid with a time stamp 1 ms after the drifted time because we told it we want ids generated after the driftTimestamp
            Assert.Equal(guid2Timestamp, driftTimestamp + 1);
        }

        [Fact]
        public void CannotProgressSeedWithoutGuidV7()
        {
            var guidV7Generator = Baubit.Identity.GuidV7Generator.CreateNew();
            Assert.Throws<InvalidOperationException>(() => guidV7Generator.InitializeFrom(Guid.NewGuid()));
        }

        [Theory]
        [InlineData(100000)]
        public void IdsAreMonotonicallyUnique(int numOfIds)
        {
            var guidV7Generator = Baubit.Identity.GuidV7Generator.CreateNew();
            var at = DateTimeOffset.Now;
            var ids = new ConcurrentBag<Guid>();
            var monotonicIds = new ConcurrentBag<Guid>();

            var parallelLoopResult = Parallel.For(0, numOfIds, _ =>
            {
                ids.Add(Identity.GuidV7.CreateVersion7(at));
                monotonicIds.Add(guidV7Generator.GetNext(at));
            });

            Assert.Null(parallelLoopResult.LowestBreakIteration);
            Assert.Equal(numOfIds, monotonicIds.Count);

            var distinctIdCount = ids.DistinctBy(id => id.ExtractTimestampMs()).Count();
            Assert.Equal(1, distinctIdCount); // By default, V7 guids all get the same timestamp if created at the same ms.

            var distinctMonotonicIdCount = monotonicIds.DistinctBy(id => id.ExtractTimestampMs()).Count();
            Assert.Equal(numOfIds, distinctMonotonicIdCount); // guids generated from GuidV7Generator have a distinct time stamp, even when generated at the same ms
        }

        [Theory]
        [InlineData(100000)]
        public void IdsAreMonotonicallyUniqueEvenWhenCreatedWithReferenceOfAnother(int numOfIds)
        {
            var monotonicIds = new ConcurrentBag<Guid>();
            var at = DateTimeOffset.UtcNow;
            var reference = Identity.GuidV7.CreateVersion7(at);
            var guidV7Generator = Baubit.Identity.GuidV7Generator.CreateNew(reference);
            var parallelLoopResult = Parallel.For(0, numOfIds, _ =>
            {
                monotonicIds.Add(guidV7Generator.GetNext(at));
            });

            Assert.Null(parallelLoopResult.LowestBreakIteration);
            Assert.Equal(numOfIds, monotonicIds.Count);

            var distinctIdCount = monotonicIds.DistinctBy(id => id.ExtractTimestampMs()).Count();

            Assert.Equal(numOfIds, distinctIdCount);
            var refTimestamp = reference.ExtractTimestampMs();
            Assert.True(monotonicIds.Select(id => id.ExtractTimestampMs()).Min() > refTimestamp); // the earliest generated guid is still - in time - later than the reference
        }

        [Fact]
        public void CanInitializeFromDateTimeOffset()
        {
            var generator = Baubit.Identity.GuidV7Generator.CreateNew();
            var futureTime = DateTimeOffset.UtcNow.AddHours(1);
            var futureMs = futureTime.ToUnixTimeMilliseconds();

            generator.InitializeFrom(futureTime);

            Assert.Equal(futureMs, generator.LastIssuedUnixMs);
        }

        [Fact]
        public void DriftCapClampsTimestamp()
        {
            var baseTime = DateTimeOffset.UtcNow;
            var reference = Identity.GuidV7.CreateVersion7(baseTime);
            var generator = Baubit.Identity.GuidV7Generator.CreateNew(reference, maxDriftMs: 10, throwOnDriftCap: false);

            // Generate many IDs at the same timestamp to force drift beyond cap
            for (int i = 0; i < 20; i++)
            {
                generator.GetNext(baseTime);
            }

            // The drift should be clamped - last issued should not exceed baseTime + 10ms
            var maxExpected = baseTime.ToUnixTimeMilliseconds() + 10;
            Assert.True(generator.LastIssuedUnixMs <= maxExpected + 1);
        }

        [Fact]
        public void DriftCapThrowsWhenConfigured()
        {
            var baseTime = DateTimeOffset.UtcNow;
            var reference = Identity.GuidV7.CreateVersion7(baseTime);
            var generator =     Baubit.Identity.GuidV7Generator.CreateNew(reference, maxDriftMs: 5, throwOnDriftCap: true);

            // Generate IDs to force drift
            Assert.Throws<InvalidOperationException>(() =>
            {
                for (int i = 0; i < 20; i++)
                {
                    generator.GetNext(baseTime);
                }
            });
        }

        [Fact]
        public void ExtractTimestampMs_ReturnsNullForNonV7Guid()
        {
            var nonV7Guid = Guid.NewGuid();
            Assert.Null(nonV7Guid.ExtractTimestampMs());
        }

        [Fact]
        public void ExtractTimestampMs_ReturnsTimestampForV7Guid()
        {
            var timestamp = DateTimeOffset.UtcNow;
            var expectedMs = timestamp.ToUnixTimeMilliseconds();
            var guid = Identity.GuidV7.CreateVersion7(timestamp);

            var extractedMs = guid.ExtractTimestampMs();

            Assert.NotNull(extractedMs);
            Assert.Equal(expectedMs, extractedMs.Value);
        }

        [Fact]
        public void MaxDriftMs_CanBeSetAndRetrieved()
        {
            var generator = Baubit.Identity.GuidV7Generator.CreateNew();

            generator.MaxDriftMs = 100;
            Assert.Equal(100, generator.MaxDriftMs);

            generator.MaxDriftMs = null;
            Assert.Null(generator.MaxDriftMs);
        }

        [Fact]
        public void ThrowOnDriftCap_CanBeSetAndRetrieved()
        {
            var generator = Baubit.Identity.GuidV7Generator.CreateNew();

            generator.ThrowOnDriftCap = true;
            Assert.True(generator.ThrowOnDriftCap);

            generator.ThrowOnDriftCap = false;
            Assert.False(generator.ThrowOnDriftCap);
        }

        [Fact]
        public void CreateNew_InitializesPropertiesCorrectly()
        {
            var generator = Baubit.Identity.GuidV7Generator.CreateNew(maxDriftMs: 50, throwOnDriftCap: true);

            Assert.Equal(50, generator.MaxDriftMs);
            Assert.True(generator.ThrowOnDriftCap);
        }

        [Fact]
        public void InitializeFrom_DoesNotRegressWithOlderTimestamp()
        {
            var futureTime = DateTimeOffset.UtcNow.AddHours(1);
            var futureGuid = Identity.GuidV7.CreateVersion7(futureTime);
            var generator = Baubit.Identity.GuidV7Generator.CreateNew(futureGuid);
            var futureMs = futureTime.ToUnixTimeMilliseconds();

            Assert.Equal(futureMs, generator.LastIssuedUnixMs);

            var pastTime = DateTimeOffset.UtcNow.AddMinutes(-30);
            var pastGuid = Identity.GuidV7.CreateVersion7(pastTime);

            generator.InitializeFrom(pastGuid);

            Assert.Equal(futureMs, generator.LastIssuedUnixMs);
        }

        [Fact]
        public void InitializeFrom_DateTimeOffset_DoesNotRegressWithOlderTimestamp()
        {
            var futureTime = DateTimeOffset.UtcNow.AddHours(1);
            var generator = Baubit.Identity.GuidV7Generator.CreateNew();
            generator.InitializeFrom(futureTime);
            var futureMs = futureTime.ToUnixTimeMilliseconds();

            Assert.Equal(futureMs, generator.LastIssuedUnixMs);

            var pastTime = DateTimeOffset.UtcNow.AddMinutes(-30);
            generator.InitializeFrom(pastTime);

            Assert.Equal(futureMs, generator.LastIssuedUnixMs);
        }

        [Fact]
        public void InitializeFrom_ThreadSafety()
        {
            var generator = Baubit.Identity.GuidV7Generator.CreateNew();
            var baseTime = DateTimeOffset.UtcNow;
            var times = new DateTimeOffset[100];

            for (int i = 0; i < times.Length; i++)
            {
                times[i] = baseTime.AddMilliseconds(i);
            }

            Parallel.ForEach(times, time =>
            {
                generator.InitializeFrom(time);
            });

            var maxExpectedMs = times.Max(t => t.ToUnixTimeMilliseconds());
            Assert.Equal(maxExpectedMs, generator.LastIssuedUnixMs);
        }

        [Fact]
        public void InitializeFrom_Guid_ThreadSafety()
        {
            var generator = Baubit.Identity.GuidV7Generator.CreateNew();
            var baseTime = DateTimeOffset.UtcNow;
            var guids = new Guid[100];

            for (int i = 0; i < guids.Length; i++)
            {
                guids[i] = Identity.GuidV7.CreateVersion7(baseTime.AddMilliseconds(i));
            }

            Parallel.ForEach(guids, guid =>
            {
                generator.InitializeFrom(guid);
            });

            var maxExpectedMs = baseTime.AddMilliseconds(guids.Length - 1).ToUnixTimeMilliseconds();
            Assert.Equal(maxExpectedMs, generator.LastIssuedUnixMs);
        }

        [Fact]
        public void TryGetUnixMs_DelegatesCorrectly()
        {
            var timestamp = DateTimeOffset.UtcNow;
            var guid = Identity.GuidV7.CreateVersion7(timestamp);

            Assert.True(Baubit.Identity.GuidV7Generator.TryGetUnixMs(guid, out long ms));
            Assert.Equal(timestamp.ToUnixTimeMilliseconds(), ms);
        }

        [Fact]
        public void TryGetUnixMs_ReturnsFalseForNonV7()
        {
            var guid = Guid.NewGuid();

            Assert.False(Baubit.Identity.GuidV7Generator.TryGetUnixMs(guid, out long ms));
            Assert.Equal(0, ms);
        }
    }
}
