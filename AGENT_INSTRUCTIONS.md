# Agent Instructions for Baubit.Identity

## Project Overview

Baubit.Identity is a standalone .NET library that provides a monotonic GuidV7 (UUIDv7) generator designed for distributed systems. This component was extracted from the main [Baubit framework](https://github.com/pnagoorkar/Baubit) as part of a componentization effort to create focused, zero-dependency packages.

## Core Purpose

The library solves a critical problem: standard `Guid.CreateVersion7()` generates identifiers with millisecond-precision timestamps that can have duplicate values when created concurrently. This causes ordering issues in distributed systems, databases with time-based sharding, and any scenario requiring guaranteed ordering.

**Baubit.Identity** ensures:
- Strictly increasing timestamps even under concurrent load
- Thread-safe generation using lock-free atomic operations
- Optional drift protection to handle clock skew
- Zero external dependencies (only .NET 9.0 standard library)

## Project Structure

```
Baubit.Identity/
├── Baubit.Identity/              # Main library project
│   ├── GuidV7Generator.cs        # Core monotonic GUID generator
│   └── Baubit.Identity.csproj    # Project file targeting .NET 9.0
├── Baubit.Identity.Tests/        # Unit tests
│   ├── GuidV7GeneratorTests.cs   # Comprehensive test suite (8 tests)
│   └── Baubit.Identity.Tests.csproj
├── .circleci/                    # CI/CD configuration
│   └── config.yml                # Build, test, pack, and publish pipeline
├── Baubit.Identity.sln           # Solution file
├── README.md                     # Comprehensive documentation
└── LICENSE                       # MIT License
```

## Key Components

### GuidV7Generator Class

**Purpose**: Generate monotonic UUIDv7 identifiers that strictly increase in timestamp order.

**Key Features**:
- Lock-free thread-safe implementation using `Volatile.Read`, `Interlocked.CompareExchange`
- Internal counter that advances timestamps to ensure uniqueness
- Optional drift protection with configurable maximum drift and behavior (throw vs clamp)
- Seeding capability from existing GuidV7 or timestamp

**Factory Methods**:
- `CreateNew()` - Create with current time
- `CreateNew(Guid existingV7)` - Seed from existing UUIDv7

**Instance Methods**:
- `GetNext()` - Generate next GUID using current UTC time
- `GetNext(DateTimeOffset)` - Generate with specific timestamp (for testing/replay)
- `InitializeFrom(Guid)` - Seed generator from existing GUID
- `InitializeFrom(DateTimeOffset)` - Seed from specific timestamp

**Static Methods**:
- `TryGetUnixMs(Guid, out long)` - Extract 48-bit Unix milliseconds from UUIDv7

### GuidV7Extensions

**Extension Method**:
- `ExtractTimestampMs(this Guid)` - Convenient extension to extract timestamp from any Guid

## Development Guidelines

### When Making Changes

1. **Preserve Zero Dependencies**: This library must have NO external dependencies beyond .NET 9.0 standard library. Do not add NuGet packages.

2. **Thread Safety is Critical**: All changes to `GuidV7Generator` must maintain thread safety. Use atomic operations, never add locks.

3. **Performance Matters**: This is designed for high-throughput scenarios. Avoid allocations, keep operations minimal.

4. **RFC 9562 Compliance**: Any changes must maintain compliance with the UUIDv7 specification.

5. **Backward Compatibility**: Public API changes require careful consideration as this is a published package.

### Testing Requirements

- All new features must have corresponding unit tests
- Tests use `xunit` framework
- Concurrent tests verify thread safety (using `ConcurrentBag<Guid>` for collection)
- All 8 existing tests must continue to pass
- Run tests with: `dotnet test`

### Build Process

- Target framework: .NET 9.0
- Build command: `dotnet build`
- Test command: `dotnet test`
- Solution file at root manages both projects

### Documentation Standards

- XML documentation comments for all public APIs
- README.md must be kept up-to-date with API changes
- Code examples in README should be working code
- Include use cases and performance characteristics

## CI/CD Pipeline

CircleCI configuration includes four jobs:

1. **Build**: Compiles the solution using custom Docker image with signing
2. **Test**: Runs all unit tests (requires Docker for Testcontainers support)
3. **Pack and Publish**: Creates NuGet package and publishes to GitHub packages (master branch only)
4. **Release**: Publishes to nuget.org (release branch only)

**Important**: Solution folder name is `Baubit.Identity` in CircleCI parameters.

## Common Tasks

### Adding a New Method to GuidV7Generator

1. Add XML documentation comment
2. Implement using lock-free operations if touching `_lastMs`
3. Add comprehensive unit tests including concurrent scenarios
4. Update README.md API reference section
5. Run full test suite to verify no regression

### Modifying Timestamp Extraction

1. Verify RFC 9562 compliance for bit layout
2. Test with edge cases (min/max timestamps)
3. Ensure `TryGetUnixMs` returns false for non-v7 GUIDs
4. Update extension method if needed

### Performance Optimization

1. Profile before optimizing
2. Avoid allocations in hot paths
3. Benchmark concurrent scenarios
4. Verify thread safety with stress tests
5. Document performance characteristics in README

## Edge Cases to Consider

- **Clock Rollback**: Generator handles by continuing from last issued timestamp
- **Drift Cap**: Optional limit prevents excessive future drift
- **Concurrent Creation**: Atomic operations ensure unique timestamps
- **Non-v7 GUIDs**: `TryGetUnixMs` properly returns false
- **Timestamp Overflow**: 48-bit timestamp valid until year ~10889

## Package Metadata

- **Package ID**: `Baubit.Identity`
- **Authors**: Prashant Nagoorkar
- **Repository**: https://github.com/pnagoorkar/Baubit.Identity
- **License**: MIT
- **Tags**: guid, uuid, guidv7, uuidv7, distributed, monotonic, timestamp

## Related Projects

- **Main Framework**: [Baubit](https://github.com/pnagoorkar/Baubit) - The framework this was extracted from
- **Component Breakdown**: See [COMPONENT_BREAKDOWN_ANALYSIS.md](https://github.com/pnagoorkar/Baubit/blob/copilot/breakdown-components-repo-structure/COMPONENT_BREAKDOWN_ANALYSIS.md) for extraction strategy

## Design Decisions

### Why Lock-Free?

Lock-free design using `Interlocked.CompareExchange` provides:
- Better scalability under high concurrency
- No risk of deadlocks
- Predictable performance characteristics
- Spin-wait is acceptable given fast operations

### Why Optional Drift Protection?

Allows users to choose between:
- **No cap**: Maximizes throughput, accepts any drift
- **Clamping**: Limits drift but continues generating IDs
- **Throwing**: Fails fast when drift exceeds threshold

Different applications have different requirements.

### Why Separate Extension Class?

`GuidV7Extensions` in separate static class follows C# conventions for extension methods while keeping core `GuidV7Generator` focused.

## Anti-Patterns to Avoid

❌ Do not add external dependencies  
❌ Do not use locks or other blocking synchronization  
❌ Do not allocate in hot paths (generator methods)  
❌ Do not break thread safety assumptions  
❌ Do not modify public API without careful consideration  
❌ Do not skip concurrent testing  
❌ Do not assume sequential execution  

## Questions or Issues?

For questions about:
- **Architecture**: Refer to component breakdown analysis
- **Usage**: Check README.md examples
- **Implementation**: Review inline code comments and tests
- **Performance**: See benchmark results in README
