# Baubit.Identity Performance Report

## System Configuration

- **OS**: Ubuntu 24.04.3 LTS (Noble Numbat)
- **CPU**: AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
- **Architecture**: X64 RyuJIT AVX2
- **.NET Runtime**: .NET 9.0.11 (9.0.1125.51716)
- **GC Mode**: Concurrent Workstation
- **Benchmark Tool**: BenchmarkDotNet v0.14.0

## Benchmark Results

### Summary

| Method | Mean | Error | StdDev | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------|------|-------|--------|-------|---------|------|-----------|-------------|
| FastGuid NewGuid() | 17.42 ns | 0.025 ns | 0.020 ns | -99% | 0.1% | 1 | - | NA |
| FastGuid NewSqlServerGuid() | 49.54 ns | 0.018 ns | 0.015 ns | -96% | 0.1% | 2 | - | NA |
| FastGuid NewPostgreSqlGuid() | 50.79 ns | 0.075 ns | 0.070 ns | -96% | 0.2% | 3 | - | NA |
| .NET 9 CreateVersion7(timestamp) | 675.45 ns | 2.510 ns | 2.225 ns | -43% | 0.3% | 4 | - | NA |
| .NET 9 CreateVersion7() | 698.53 ns | 1.490 ns | 1.244 ns | -41% | 0.2% | 5 | - | NA |
| Baubit Generator GetNext(timestamp) | 1,160.19 ns | 4.231 ns | 3.751 ns | -3% | 0.3% | 6 | - | NA |
| Baubit CreateVersion7(timestamp) | 1,178.69 ns | 1.684 ns | 1.493 ns | -1% | 0.2% | 6 | - | NA |
| Baubit CreateVersion7() | 1,190.92 ns | 1.382 ns | 1.225 ns | baseline | - | 6 | - | NA |
| Baubit Generator GetNext() | 1,194.04 ns | 2.757 ns | 2.152 ns | +0% | 0.2% | 6 | - | NA |

### Analysis

#### FastGuid Comparison

**FastGuid Performance**
- **NewGuid()**: 17.42 ns ± 0.025 ns - Random GUID generation
- **NewSqlServerGuid()**: 49.54 ns ± 0.018 ns - SQL Server-optimized sequential GUID
- **NewPostgreSqlGuid()**: 50.79 ns ± 0.075 ns - PostgreSQL-optimized sequential GUID

FastGuid demonstrates exceptional raw performance for GUID generation, being **68x faster** than Baubit.Identity for random GUIDs. The database-optimized variants (NewSqlServerGuid and NewPostgreSqlGuid) are approximately **24x faster** than Baubit.Identity's UUIDv7 implementation.

**Key Differences**:
- FastGuid.NewGuid() generates random GUIDs without timestamp information
- FastGuid's database-optimized GUIDs use custom byte ordering for specific database engines
- Baubit.Identity focuses on RFC 9562-compliant UUIDv7 with embedded timestamps and monotonic guarantees

#### Without Timestamp (Current Time)

**Baubit.Identity (Baseline)**
- **CreateVersion7()**: 1,190.92 ns ± 1.38 ns
- **Generator GetNext()**: 1,194.04 ns ± 2.76 ns (monotonic guarantee)

**.NET 9 Built-in**
- **CreateVersion7()**: 698.53 ns ± 1.49 ns
- **41% faster** than Baubit's static method
- **41% faster** than Baubit's monotonic generator

#### With Fixed Timestamp

**Baubit.Identity**
- **CreateVersion7(timestamp)**: 1,178.69 ns ± 1.68 ns
- **Generator GetNext(timestamp)**: 1,160.19 ns ± 4.23 ns (monotonic guarantee)

**.NET 9 Built-in**
- **CreateVersion7(timestamp)**: 675.45 ns ± 2.51 ns
- **43% faster** than Baubit's static method
- **42% faster** than Baubit's monotonic generator

### Key Observations

1. **Zero Allocations**: All methods produce zero heap allocations per operation, making them suitable for high-throughput scenarios.

2. **FastGuid Speed**: FastGuid excels at raw GUID generation speed, being 68x faster for random GUIDs and 24x faster for database-optimized sequential GUIDs. However, FastGuid's optimizations are database-specific and don't follow the UUIDv7 RFC 9562 standard.

3. **Monotonic Guarantee Trade-off**: Baubit's `GuidV7Generator.GetNext()` provides strict monotonicity (guaranteed increasing order even at the same millisecond) at minimal performance cost compared to the non-monotonic static method.

4. **.NET Standard 2.0 Compatibility**: Baubit.Identity uses `RandomNumberGenerator` for cryptographically secure random number generation with thread-local caching, which explains the performance difference compared to .NET 9's native implementation and FastGuid's optimized approach.

5. **Standards Compliance**: Baubit.Identity prioritizes RFC 9562 UUIDv7 compliance with embedded timestamps and version bits, while FastGuid prioritizes raw performance with custom GUID formats for specific database engines.

6. **Use Case Differentiation**:
   - Use **FastGuid** when you need maximum performance and don't require RFC-compliant UUIDv7 or extractable timestamps
   - Use **Baubit.Identity** when you need standards-compliant UUIDv7 with embedded timestamps, broad .NET compatibility, and monotonic guarantees

### Performance Characteristics

- **Throughput**: 
  - FastGuid: ~57 million GUIDs per second (NewGuid), ~20 million per second (database-optimized)
  - Baubit.Identity: ~840,000 GUIDs per second (single-threaded)
  - .NET 9: ~1.4 million GUIDs per second
- **Memory**: Zero heap allocations per operation for all implementations
- **Thread Safety**: Lock-free atomic operations with minimal contention
- **Latency**: 
  - FastGuid: Sub-100 nanosecond latency
  - Baubit.Identity: ~1.2 microsecond latency with low variance

### Recommendations

- Use **FastGuid** when:
  - You need maximum raw performance for GUID generation
  - You're using SQL Server or PostgreSQL and want database-optimized sequential GUIDs
  - You don't need RFC 9562 UUIDv7 compliance
  - Timestamp extraction from the GUID is not required

- Use **Baubit.Identity** when:
  - You need RFC 9562-compliant UUIDv7 with embedded timestamps
  - Strict monotonicity is required for distributed systems or database indexing
  - Broad .NET compatibility (.NET Standard 2.0) is important
  - You need to extract timestamps from generated GUIDs
  - Standards compliance and interoperability with other UUIDv7 implementations matter

- Use **.NET 9 Guid.CreateVersion7()** when:
  - You're on .NET 9+ and need a good balance of performance and UUIDv7 compliance
  - Monotonic guarantees are not critical

## Running Benchmarks

To reproduce these results:

```bash
cd Baubit.Identity.Benchmarks
dotnet run -c Release
```

## Conclusion

Baubit.Identity delivers production-grade performance with zero allocations while maintaining .NET Standard 2.0 compatibility and RFC 9562 UUIDv7 compliance. 

**Performance Trade-offs**:
- FastGuid offers superior raw performance (68x faster for random GUIDs) but sacrifices RFC compliance and timestamp extraction capabilities
- .NET 9's built-in CreateVersion7() provides a middle ground with ~1.7x better performance than Baubit.Identity while maintaining UUIDv7 compliance
- Baubit.Identity prioritizes standards compliance, broad .NET compatibility, and monotonic guarantees over raw performance

The monotonic generator provides strict ordering guarantees with minimal performance overhead, making it ideal for distributed systems requiring time-sortable, standards-compliant identifiers across different .NET platforms.
