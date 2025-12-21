# Baubit.Identity

[![CircleCI](https://dl.circleci.com/status-badge/img/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master.svg?style=svg)](https://dl.circleci.com/status-badge/redirect/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master)
[![codecov](https://codecov.io/gh/pnagoorkar/Baubit.Identity/branch/master/graph/badge.svg)](https://codecov.io/gh/pnagoorkar/Baubit.Identity)<br/>
[![NuGet](https://img.shields.io/nuget/v/Baubit.Identity.svg)](https://www.nuget.org/packages/Baubit.Identity/)
[![NuGet](https://img.shields.io/nuget/dt/Baubit.Identity.svg)](https://www.nuget.org/packages/Baubit.Identity) <br/>
![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4?logo=dotnet&logoColor=white)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)<br/>
[![Known Vulnerabilities](https://snyk.io/test/github/pnagoorkar/Baubit.Identity/badge.svg)](https://snyk.io/test/github/pnagoorkar/Baubit.Identity)

A monotonic GuidV7 (UUIDv7) generator for .NET. Generates strictly increasing unique identifiers for distributed systems.

## Features

- **Interface-based Design**: `IIdentityGenerator` interface for abstraction and testability
- **Monotonic Guarantees**: Strictly increasing UUIDv7 identifiers, even at the same millisecond
- **Thread-Safe**: Lock-free implementation using atomic operations
- **Drift Protection**: Optional drift cap for clock skew handling
- **Timestamp Extraction**: Extract Unix milliseconds from GuidV7 values
- **Zero Dependencies**: No external dependencies
- **RFC 9562 Compliant**: Follows UUIDv7 specification

## Installation

```bash
dotnet add package Baubit.Identity
```

## Quick Start

```csharp
using Baubit.Identity;

// Create a generator using the interface
IIdentityGenerator generator = IdentityGenerator.CreateNew();

// Generate monotonic GuidV7 values
var id1 = generator.GetNext();
var id2 = generator.GetNext();
// id1 < id2 (strictly increasing)

// Seed from existing GUID or timestamp
var existingId = GuidV7.CreateVersion7();
generator.InitializeFrom(existingId);

// Or seed from timestamp
generator.InitializeFrom(DateTimeOffset.UtcNow.AddHours(-1));

// Create UUIDv7 directly
var guid = GuidV7.CreateVersion7();

// Extract timestamp
if (GuidV7.TryGetUnixMs(guid, out long ms))
{
    var time = DateTimeOffset.FromUnixTimeMilliseconds(ms);
}
```

## API Reference

### IIdentityGenerator (Interface)

- `GetNext()` - Generate next monotonic GUID using current UTC time
- `GetNext(DateTimeOffset timestampUtc)` - Generate monotonic GUID with specific timestamp
- `InitializeFrom(Guid existingV7)` - Seed from existing version 7 GUID
- `InitializeFrom(DateTimeOffset timestampUtc)` - Seed from specific UTC timestamp

### IdentityGenerator

- `CreateNew(long? maxDriftMs = null, bool throwOnDriftCap = false)` - Create generator with optional drift protection
- `CreateNew(Guid existingV7, long? maxDriftMs = null, bool throwOnDriftCap = false)` - Create generator seeded from existing UUIDv7
- `GetNext()` - Generate next monotonic GUID
- `GetNext(DateTimeOffset timestampUtc)` - Generate monotonic GUID with specific timestamp
- `InitializeFrom(Guid existingV7)` - Seed from existing version 7 GUID
- `InitializeFrom(DateTimeOffset timestampUtc)` - Seed from specific UTC timestamp

### GuidV7 (Static)

- `CreateVersion7()` - Create UUIDv7 with current UTC time
- `CreateVersion7(DateTimeOffset timestamp)` - Create UUIDv7 with specific timestamp
- `IsVersion7(Guid guid)` - Check if GUID is version 7
- `TryGetUnixMs(Guid guid, out long ms)` - Extract timestamp from UUIDv7

### GuidV7Generator

- `CreateNew(long? maxDriftMs, bool throwOnDriftCap)` - Create generator
- `CreateNew(Guid existingV7, ...)` - Create generator seeded from existing UUIDv7
- `GetNext()` - Generate next monotonic GuidV7
- `GetNext(DateTimeOffset timestampUtc)` - Generate with specific timestamp
- `InitializeFrom(Guid existingV7)` - Seed from existing UUIDv7
- `InitializeFrom(DateTimeOffset timestampUtc)` - Seed from timestamp

### GuidV7Extensions

- `ExtractTimestampMs(this Guid guid)` - Extension to extract timestamp

## Performance

Benchmarks comparing Baubit.Identity against .NET 9's built-in `Guid.CreateVersion7()` (Intel Core Ultra 9 185H, Windows 11):

| Method | Mean | Ratio | Allocated |
|--------|------|-------|-----------|
| .NET 9 CreateVersion7() | 67.09 ns | -11% | 0 B |
| Baubit CreateVersion7() | 75.40 ns | baseline | 0 B |
| Baubit Generator GetNext() | 78.54 ns | +4% | 0 B |

**Key Highlights**:
- **Zero allocations** - All methods produce zero heap allocations
- **Competitive performance** - Within 11% of .NET 9's native implementation
- **Monotonic guarantee** - Generator provides strict ordering at only 4% cost
- **.NET Standard 2.0** - Broad compatibility with minimal performance trade-off

For detailed benchmark results, analysis, and hardware specifications, see [Performance Report](Baubit.Identity.Benchmarks/PERFORMANCE.md).

**Note**: Baubit.Identity targets .NET Standard 2.0 for broad compatibility. The implementation uses cryptographically secure random number generation (`RandomNumberGenerator`) with thread-local caching for zero allocations.

## License

MIT License - see [LICENSE](LICENSE)
