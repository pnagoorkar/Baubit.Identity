# Baubit.Identity Performance Report

## System Configuration

- **OS**: Windows 11 (10.0.26200.7171)
- **CPU**: Intel(R) Core(TM) Ultra 9 185H
- **Architecture**: X64 RyuJIT AVX2
- **.NET Runtime**: .NET 9.0.11 (9.0.1125.51716)
- **GC Mode**: Non-concurrent Workstation
- **Benchmark Tool**: BenchmarkDotNet v0.14.0

## Benchmark Results

### Summary

| Method | Mean | Error | StdDev | Median | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------|------|-------|--------|--------|-------|---------|------|-----------|-------------|
| .NET 9 CreateVersion7(timestamp) | 43.62 ns | 0.342 ns | 0.320 ns | 43.63 ns | -42% | 1.0% | 1 | - | NA |
| Baubit CreateVersion7(timestamp) | 52.11 ns | 0.319 ns | 0.298 ns | 52.06 ns | -31% | 0.9% | 2 | - | NA |
| Baubit Generator GetNext(timestamp) | 56.30 ns | 0.415 ns | 0.388 ns | 56.19 ns | -25% | 1.0% | 3 | - | NA |
| .NET 9 CreateVersion7() | 67.09 ns | 1.355 ns | 2.611 ns | 65.63 ns | -11% | 3.9% | 4 | - | NA |
| Baubit CreateVersion7() | 75.40 ns | 0.675 ns | 0.564 ns | 75.44 ns | baseline | - | 5 | - | NA |
| Baubit Generator GetNext() | 78.54 ns | 1.003 ns | 0.938 ns | 78.70 ns | +4% | 1.4% | 6 | - | NA |

### Analysis

#### Without Timestamp (Current Time)

**Baubit.Identity (Baseline)**
- **CreateVersion7()**: 75.40 ns ± 0.68 ns
- **Generator GetNext()**: 78.54 ns ± 1.00 ns (monotonic guarantee)

**NET 9 Built-in**
- **CreateVersion7()**: 67.09 ns ± 1.36 ns
- **11% faster** than Baubit's static method
- **15% faster** than Baubit's monotonic generator

#### With Fixed Timestamp

**Baubit.Identity**
- **CreateVersion7(timestamp)**: 52.11 ns ± 0.32 ns
- **Generator GetNext(timestamp)**: 56.30 ns ± 0.42 ns (monotonic guarantee)

**.NET 9 Built-in**
- **CreateVersion7(timestamp)**: 43.62 ns ± 0.34 ns
- **16% faster** than Baubit's static method
- **23% faster** than Baubit's monotonic generator

### Key Observations

1. **Zero Allocations**: All methods produce zero heap allocations per operation, making them suitable for high-throughput scenarios.

2. **Monotonic Guarantee Trade-off**: Baubit's `GuidV7Generator.GetNext()` provides strict monotonicity (guaranteed increasing order even at the same millisecond) at only ~4% performance cost compared to the non-monotonic static method.

3. **Competitive Performance**: Baubit.Identity performs within 11-23% of .NET 9's built-in implementation while targeting .NET Standard 2.0 for broad compatibility.

4. **.NET Standard 2.0 Compatibility**: Baubit.Identity uses `RandomNumberGenerator` for cryptographically secure random number generation with thread-local caching, which explains the slight performance difference compared to .NET 9's native implementation.

5. **Fixed Timestamp Performance**: When using a pre-computed timestamp, both implementations are faster (43-56 ns range) compared to generating timestamps dynamically (67-79 ns range).

### Performance Characteristics

- **Throughput**: Approximately 12-23 million GUIDs per second (single-threaded)
- **Memory**: Zero heap allocations per operation
- **Thread Safety**: Lock-free atomic operations with minimal contention
- **Latency**: Consistent sub-100 nanosecond latency with low variance

### Recommendations

- Use **`GuidV7Generator.GetNext()`** when strict monotonicity is required (distributed systems, database indexing)
- Use **`GuidV7.CreateVersion7()`** when monotonicity is not critical and maximum performance is desired
- Pre-compute timestamps when generating multiple GUIDs at the same logical time point
- Both implementations are suitable for high-throughput production workloads

## Running Benchmarks

To reproduce these results:

```bash
cd Baubit.Identity.Benchmarks
dotnet run -c Release
```

## Comparison with Previous Results

Previous benchmarks (run on GitHub CI agent) showed significantly different performance characteristics:

| Method (GitHub CI) | Mean | Allocated |
|--------|------|-----------|
| .NET 9 CreateVersion7() | 704 ns | 0 B |
| Baubit CreateVersion7() | 1,195 ns | 0 B |
| Baubit Generator GetNext() | 1,194 ns | 0 B |

The local Intel Core Ultra 9 185H shows **9-16x better performance** compared to the GitHub CI agent, demonstrating the impact of hardware capabilities on GUID generation performance. This highlights the importance of running benchmarks on representative hardware.

## Conclusion

Baubit.Identity delivers production-grade performance with zero allocations while maintaining .NET Standard 2.0 compatibility. The monotonic generator provides strict ordering guarantees with minimal performance overhead, making it ideal for distributed systems and scenarios requiring time-sortable identifiers.
