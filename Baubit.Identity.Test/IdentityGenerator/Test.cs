using System.Collections.Concurrent;

namespace Baubit.Identity.Test.IdentityGenerator
{
    public class Test
    {
        [Fact]
        public void CanCreateDefault()
        {
            var generator = Baubit.Identity.IdentityGenerator.CreateNew();
            Assert.NotNull(generator);
        }

        [Fact]
        public void CanCreateUsingGuidV7()
        {
            var dateTimeOffset = DateTimeOffset.UtcNow;
            var reference = Identity.GuidV7.CreateVersion7(dateTimeOffset);
            var generator = Baubit.Identity.IdentityGenerator.CreateNew(reference);
            Assert.NotNull(generator);
        }

        [Fact]
        public void CanNotCreateWithoutGuidV7()
        {
            Assert.Throws<InvalidOperationException>(() => Baubit.Identity.IdentityGenerator.CreateNew(Guid.NewGuid()));
        }

        [Fact]
        public void GetNext_ReturnsValidGuid()
        {
            var generator = Baubit.Identity.IdentityGenerator.CreateNew();
            var guid = generator.GetNext();
            
            Assert.NotEqual(Guid.Empty, guid);
            Assert.True(Identity.GuidV7.IsVersion7(guid));
        }

        [Fact]
        public void GetNext_WithTimestamp_ReturnsValidGuid()
        {
            var generator = Baubit.Identity.IdentityGenerator.CreateNew();
            var timestamp = DateTimeOffset.UtcNow;
            var guid = generator.GetNext(timestamp);
            
            Assert.NotEqual(Guid.Empty, guid);
            Assert.True(Identity.GuidV7.IsVersion7(guid));
        }

        [Fact]
        public void GetNext_GeneratesMonotonicGuids()
        {
            var generator = Baubit.Identity.IdentityGenerator.CreateNew();
            var guid1 = generator.GetNext();
            var guid2 = generator.GetNext();
            
            var ms1 = guid1.ExtractTimestampMs();
            var ms2 = guid2.ExtractTimestampMs();
            
            Assert.NotNull(ms1);
            Assert.NotNull(ms2);
            Assert.True(ms2 > ms1);
        }

        [Fact]
        public void InitializeFrom_Guid_SeedsGenerator()
        {
            var generator = Baubit.Identity.IdentityGenerator.CreateNew();
            var futureTime = DateTimeOffset.UtcNow.AddHours(1);
            var futureGuid = Identity.GuidV7.CreateVersion7(futureTime);
            
            generator.InitializeFrom(futureGuid);
            
            var nextGuid = generator.GetNext();
            var nextMs = nextGuid.ExtractTimestampMs();
            var futureMs = futureGuid.ExtractTimestampMs();
            
            Assert.NotNull(nextMs);
            Assert.NotNull(futureMs);
            Assert.True(nextMs > futureMs);
        }

        [Fact]
        public void InitializeFrom_DateTimeOffset_SeedsGenerator()
        {
            var generator = Baubit.Identity.IdentityGenerator.CreateNew();
            var futureTime = DateTimeOffset.UtcNow.AddHours(1);
            var futureMs = futureTime.ToUnixTimeMilliseconds();
            
            generator.InitializeFrom(futureTime);
            
            var nextGuid = generator.GetNext();
            var nextMs = nextGuid.ExtractTimestampMs();
            
            Assert.NotNull(nextMs);
            Assert.True(nextMs > futureMs);
        }

        [Fact]
        public void InitializeFrom_ThrowsForNonV7Guid()
        {
            var generator = Baubit.Identity.IdentityGenerator.CreateNew();
            Assert.Throws<InvalidOperationException>(() => generator.InitializeFrom(Guid.NewGuid()));
        }

        [Fact]
        public void InitializeFrom_Guid_DoesNotRegressWithOlderTimestamp()
        {
            var futureTime = DateTimeOffset.UtcNow.AddHours(1);
            var futureGuid = Identity.GuidV7.CreateVersion7(futureTime);
            var generator = Baubit.Identity.IdentityGenerator.CreateNew(futureGuid);
            var futureMs = futureTime.ToUnixTimeMilliseconds();
            
            var pastTime = DateTimeOffset.UtcNow.AddMinutes(-30);
            var pastGuid = Identity.GuidV7.CreateVersion7(pastTime);
            
            generator.InitializeFrom(pastGuid);
            
            var nextGuid = generator.GetNext();
            var nextMs = nextGuid.ExtractTimestampMs();
            
            Assert.NotNull(nextMs);
            Assert.True(nextMs >= futureMs);
        }

        [Fact]
        public void InitializeFrom_DateTimeOffset_DoesNotRegressWithOlderTimestamp()
        {
            var futureTime = DateTimeOffset.UtcNow.AddHours(1);
            var generator = Baubit.Identity.IdentityGenerator.CreateNew();
            generator.InitializeFrom(futureTime);
            var futureMs = futureTime.ToUnixTimeMilliseconds();
            
            var pastTime = DateTimeOffset.UtcNow.AddMinutes(-30);
            generator.InitializeFrom(pastTime);
            
            var nextGuid = generator.GetNext();
            var nextMs = nextGuid.ExtractTimestampMs();
            
            Assert.NotNull(nextMs);
            Assert.True(nextMs >= futureMs);
        }

        [Theory]
        [InlineData(100000)]
        public void GetNext_GeneratesMonotonicallyUniqueGuidsUnderConcurrency(int numOfIds)
        {
            var generator = Baubit.Identity.IdentityGenerator.CreateNew();
            var at = DateTimeOffset.Now;
            var monotonicIds = new ConcurrentBag<Guid>();

            var parallelLoopResult = Parallel.For(0, numOfIds, _ =>
            {
                monotonicIds.Add(generator.GetNext(at));
            });

            Assert.Null(parallelLoopResult.LowestBreakIteration);
            Assert.Equal(numOfIds, monotonicIds.Count);

            // Verify all GUIDs are fully unique
            var distinctGuidCount = monotonicIds.Distinct().Count();
            Assert.Equal(numOfIds, distinctGuidCount);

            // Verify monotonic timestamps - each GUID has a distinct timestamp
            var distinctMonotonicIdCount = monotonicIds.DistinctBy(id => id.ExtractTimestampMs()).Count();
            Assert.Equal(numOfIds, distinctMonotonicIdCount);
        }

        [Fact]
        public void InitializeFrom_Guid_ThreadSafety()
        {
            var generator = Baubit.Identity.IdentityGenerator.CreateNew();
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

            var nextGuid = generator.GetNext();
            var nextMs = nextGuid.ExtractTimestampMs();
            var maxExpectedMs = baseTime.AddMilliseconds(guids.Length - 1).ToUnixTimeMilliseconds();
            
            Assert.NotNull(nextMs);
            Assert.True(nextMs >= maxExpectedMs);
        }

        [Fact]
        public void InitializeFrom_DateTimeOffset_ThreadSafety()
        {
            var generator = Baubit.Identity.IdentityGenerator.CreateNew();
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

            var nextGuid = generator.GetNext();
            var nextMs = nextGuid.ExtractTimestampMs();
            var maxExpectedMs = times.Max(t => t.ToUnixTimeMilliseconds());
            
            Assert.NotNull(nextMs);
            Assert.True(nextMs >= maxExpectedMs);
        }

        [Fact]
        public void CreateNew_WithDriftCap_ClampsTimestamp()
        {
            var baseTime = DateTimeOffset.UtcNow;
            var reference = Identity.GuidV7.CreateVersion7(baseTime);
            var generator = Baubit.Identity.IdentityGenerator.CreateNew(reference, maxDriftMs: 10, throwOnDriftCap: false);

            for (int i = 0; i < 20; i++)
            {
                generator.GetNext(baseTime);
            }

            var nextGuid = generator.GetNext(baseTime);
            Assert.NotEqual(Guid.Empty, nextGuid);
        }

        [Fact]
        public void CreateNew_WithThrowOnDriftCap_ThrowsWhenExceeded()
        {
            var baseTime = DateTimeOffset.UtcNow;
            var reference = Identity.GuidV7.CreateVersion7(baseTime);
            var generator = Baubit.Identity.IdentityGenerator.CreateNew(reference, maxDriftMs: 5, throwOnDriftCap: true);

            Assert.Throws<InvalidOperationException>(() =>
            {
                for (int i = 0; i < 20; i++)
                {
                    generator.GetNext(baseTime);
                }
            });
        }

        [Fact]
        public void ImplementsIIdentityGenerator()
        {
            var generator = Baubit.Identity.IdentityGenerator.CreateNew();
            Assert.IsAssignableFrom<IIdentityGenerator>(generator);
        }

        [Fact]
        public void Interface_InitializeFrom_Guid_Works()
        {
            IIdentityGenerator generator = Baubit.Identity.IdentityGenerator.CreateNew();
            var futureTime = DateTimeOffset.UtcNow.AddHours(1);
            var futureGuid = Identity.GuidV7.CreateVersion7(futureTime);
            
            generator.InitializeFrom(futureGuid);
            
            var nextGuid = generator.GetNext();
            var nextMs = nextGuid.ExtractTimestampMs();
            var futureMs = futureGuid.ExtractTimestampMs();
            
            Assert.NotNull(nextMs);
            Assert.NotNull(futureMs);
            Assert.True(nextMs > futureMs);
        }

        [Fact]
        public void Interface_InitializeFrom_DateTimeOffset_Works()
        {
            IIdentityGenerator generator = Baubit.Identity.IdentityGenerator.CreateNew();
            var futureTime = DateTimeOffset.UtcNow.AddHours(1);
            var futureMs = futureTime.ToUnixTimeMilliseconds();
            
            generator.InitializeFrom(futureTime);
            
            var nextGuid = generator.GetNext();
            var nextMs = nextGuid.ExtractTimestampMs();
            
            Assert.NotNull(nextMs);
            Assert.True(nextMs > futureMs);
        }

        [Fact]
        public void Interface_GetNext_Works()
        {
            IIdentityGenerator generator = Baubit.Identity.IdentityGenerator.CreateNew();
            var guid = generator.GetNext();
            
            Assert.NotEqual(Guid.Empty, guid);
            Assert.True(Identity.GuidV7.IsVersion7(guid));
        }

        [Fact]
        public void Interface_GetNext_WithTimestamp_Works()
        {
            IIdentityGenerator generator = Baubit.Identity.IdentityGenerator.CreateNew();
            var timestamp = DateTimeOffset.UtcNow;
            var guid = generator.GetNext(timestamp);
            
            Assert.NotEqual(Guid.Empty, guid);
            Assert.True(Identity.GuidV7.IsVersion7(guid));
        }

        [Fact]
        public void CreateNew_WithExistingV7_InitializesCorrectly()
        {
            var dateTimeOffset = DateTimeOffset.UtcNow;
            var expectedMs = dateTimeOffset.ToUnixTimeMilliseconds();
            var reference = Identity.GuidV7.CreateVersion7(dateTimeOffset);
            var generator = Baubit.Identity.IdentityGenerator.CreateNew(reference);
            
            var nextGuid = generator.GetNext();
            var nextMs = nextGuid.ExtractTimestampMs();
            
            Assert.NotNull(nextMs);
            Assert.True(nextMs > expectedMs);
        }

        [Fact]
        public void CreateNew_WithParameters_InitializesCorrectly()
        {
            var generator = Baubit.Identity.IdentityGenerator.CreateNew(maxDriftMs: 50, throwOnDriftCap: true);
            Assert.NotNull(generator);
        }

        [Theory]
        [InlineData(100000)]
        public void GetNext_GeneratesUniqueGuidsWhenCreatedWithReference(int numOfIds)
        {
            var at = DateTimeOffset.UtcNow;
            var reference = Identity.GuidV7.CreateVersion7(at);
            var generator = Baubit.Identity.IdentityGenerator.CreateNew(reference);
            var monotonicIds = new ConcurrentBag<Guid>();
            
            var parallelLoopResult = Parallel.For(0, numOfIds, _ =>
            {
                monotonicIds.Add(generator.GetNext(at));
            });

            Assert.Null(parallelLoopResult.LowestBreakIteration);
            Assert.Equal(numOfIds, monotonicIds.Count);

            // Verify all GUIDs are fully unique
            var distinctGuidCount = monotonicIds.Distinct().Count();
            Assert.Equal(numOfIds, distinctGuidCount);

            // Verify monotonic timestamps - each GUID has a distinct timestamp
            var distinctIdCount = monotonicIds.DistinctBy(id => id.ExtractTimestampMs()).Count();
            Assert.Equal(numOfIds, distinctIdCount);
            
            var refTimestamp = reference.ExtractTimestampMs();
            Assert.True(monotonicIds.Select(id => id.ExtractTimestampMs()).Min() > refTimestamp);
        }
    }
}
