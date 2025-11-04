# Baubit.Identity

[![CircleCI](https://dl.circleci.com/status-badge/img/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master.svg?style=svg)](https://dl.circleci.com/status-badge/redirect/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master)
[![codecov](https://codecov.io/gh/pnagoorkar/Baubit.Identity/branch/master/graph/badge.svg)](https://codecov.io/gh/pnagoorkar/Baubit.Identity)

A lightweight, high-performance monotonic GuidV7 (UUIDv7) generator for .NET 9.0+ applications, designed for distributed systems that require strictly increasing unique identifiers.

## Features

- **Monotonic Guarantees**: Generates strictly increasing UUIDv7 identifiers, even when created at the same millisecond
- **Thread-Safe**: Lock-free implementation using `Interlocked` operations for high-performance concurrent access
- **Drift Protection**: Built-in optional drift cap to prevent clock skew issues in distributed systems
- **Timestamp Extraction**: Extension methods to extract Unix milliseconds from GuidV7 values
- **Zero Dependencies**: No external dependencies beyond .NET 9.0 standard library
- **Compliant**: Follows RFC 9562 UUIDv7 specification

## Installation

```bash
dotnet add package Baubit.Identity
```

## Quick Start

### Basic Usage

```csharp
using Baubit.Identity;

// Create a new generator
var generator = GuidV7Generator.CreateNew();

// Generate monotonic GuidV7 values
var id1 = generator.GetNext();
var id2 = generator.GetNext();
var id3 = generator.GetNext();

// id1 < id2 < id3 (strictly increasing)
```

### Seeding from Existing GuidV7

```csharp
// Create a generator seeded from an existing GuidV7
var existingId = Guid.CreateVersion7();
var generator = GuidV7Generator.CreateNew(existingId);

// Future IDs will be strictly greater than the seed
var nextId = generator.GetNext();
```

### Drift Protection

```csharp
// Create generator with drift cap (prevents future drift > 1000ms)
var generator = GuidV7Generator.CreateNew(
    maxDriftMs: 1000,      // Maximum 1 second drift
    throwOnDriftCap: true  // Throw exception if cap exceeded
);

var id = generator.GetNext();
```

### Extracting Timestamps

```csharp
var id = generator.GetNext();

// Extract timestamp using extension method
long? timestampMs = id.ExtractTimestampMs();

// Or use the static method
if (GuidV7Generator.TryGetUnixMs(id, out long ms))
{
    var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(ms);
    Console.WriteLine($"ID created at: {dateTime}");
}
```

## Why Monotonic GuidV7?

Standard `Guid.CreateVersion7()` generates identifiers with millisecond-precision timestamps. When multiple GUIDs are created within the same millisecond, they may not maintain strict ordering. This can cause issues in distributed systems, databases with time-based sharding, or any scenario requiring guaranteed ordering.

**Baubit.Identity** solves this by maintaining an internal counter that ensures each generated GUID has a strictly increasing timestamp, even when created concurrently at the same millisecond.

### Comparison

```csharp
var timestamp = DateTimeOffset.UtcNow;

// Standard GuidV7 - may have duplicate timestamps
var standardIds = new ConcurrentBag<Guid>();
Parallel.For(0, 100, _ => standardIds.Add(Guid.CreateVersion7(timestamp)));
var distinctTimestamps = standardIds.Select(id => id.ExtractTimestampMs()).Distinct().Count();
// distinctTimestamps == 1 (all have same timestamp)

// Monotonic GuidV7 - guaranteed unique timestamps
var generator = GuidV7Generator.CreateNew();
var monotonicIds = new ConcurrentBag<Guid>();
Parallel.For(0, 100, _ => monotonicIds.Add(generator.GetNext(timestamp)));
var distinctMonotonic = monotonicIds.Select(id => id.ExtractTimestampMs()).Distinct().Count();
// distinctMonotonic == 100 (each has unique timestamp)
```

## API Reference

### GuidV7Generator

#### Static Factory Methods

- `CreateNew(long? maxDriftMs = null, bool throwOnDriftCap = false)` - Creates a new generator with current timestamp
- `CreateNew(Guid existingV7, long? maxDriftMs = null, bool throwOnDriftCap = false)` - Creates a generator seeded from an existing UUIDv7

#### Instance Methods

- `GetNext()` - Generate next monotonic GuidV7 using current UTC time
- `GetNext(DateTimeOffset timestampUtc)` - Generate using a specific timestamp
- `InitializeFrom(Guid existingV7)` - Seed generator from existing UUIDv7
- `InitializeFrom(DateTimeOffset timestampUtc)` - Seed generator from specific timestamp

#### Static Methods

- `TryGetUnixMs(Guid guid, out long ms)` - Extract Unix milliseconds from a UUIDv7

#### Properties

- `MaxDriftMs` - Maximum allowed drift ahead of wall-clock (milliseconds)
- `ThrowOnDriftCap` - Whether to throw exception when drift cap is exceeded
- `LastIssuedUnixMs` - Last issued timestamp (for diagnostics/testing)

### GuidV7Extensions

- `ExtractTimestampMs(this Guid guid)` - Extension method to extract timestamp from any Guid

## Use Cases

- **Distributed Systems**: Generate unique, sortable identifiers across multiple nodes
- **Database Keys**: Use as primary keys with natural time-based ordering
- **Event Sourcing**: Maintain strict event ordering even with high throughput
- **Log Aggregation**: Sortable identifiers for distributed log entries
- **Message Queues**: Priority-based ordering using timestamps
- **API Request IDs**: Traceable, time-ordered request identifiers

## Performance

The generator uses lock-free atomic operations (`Volatile.Read`, `Interlocked.CompareExchange`) for thread safety, providing excellent performance characteristics:

- Zero memory allocations per ID generation
- No lock contention under concurrent load
- Suitable for high-throughput scenarios

## Thread Safety

`GuidV7Generator` is fully thread-safe and can be shared across multiple threads. The internal state is protected using atomic operations.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! This component was extracted from the [Baubit](https://github.com/pnagoorkar/Baubit) framework as part of a componentization effort.

## Related Projects

- [Baubit](https://github.com/pnagoorkar/Baubit) - The main framework this component was extracted from

## References

- [RFC 9562: Universally Unique IDentifiers (UUIDs)](https://www.rfc-editor.org/rfc/rfc9562.html)
- [UUIDv7 Specification](https://www.rfc-editor.org/rfc/rfc9562.html#name-uuid-version-7)