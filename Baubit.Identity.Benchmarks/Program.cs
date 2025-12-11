using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using SecurityDriven;

namespace Baubit.Identity.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        var config = ManualConfig.CreateMinimumViable()
            .WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Percentage))
            .AddColumn(RankColumn.Arabic)
            .WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));
        
        BenchmarkRunner.Run<GuidV7Benchmarks>(config, args);
    }
}

/// <summary>
/// Benchmarks comparing Baubit.Identity GuidV7 implementation against .NET 9's built-in Guid.CreateVersion7
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class GuidV7Benchmarks
{
    private GuidV7Generator _generator = null!;
    private DateTimeOffset _fixedTimestamp;

    [GlobalSetup]
    public void Setup()
    {
        _generator = GuidV7Generator.CreateNew();
        _fixedTimestamp = DateTimeOffset.UtcNow;
    }

    // ---- CreateVersion7() without timestamp ----

    [Benchmark(Description = ".NET 9 CreateVersion7()")]
    public Guid DotNet9_CreateVersion7()
    {
        return Guid.CreateVersion7();
    }

    [Benchmark(Description = "Baubit CreateVersion7()", Baseline = true)]
    public Guid Baubit_CreateVersion7()
    {
        return GuidV7.CreateVersion7();
    }

    // ---- CreateVersion7(timestamp) ----

    [Benchmark(Description = ".NET 9 CreateVersion7(timestamp)")]
    public Guid DotNet9_CreateVersion7_WithTimestamp()
    {
        return Guid.CreateVersion7(_fixedTimestamp);
    }

    [Benchmark(Description = "Baubit CreateVersion7(timestamp)")]
    public Guid Baubit_CreateVersion7_WithTimestamp()
    {
        return GuidV7.CreateVersion7(_fixedTimestamp);
    }

    // ---- Generator (monotonic) ----

    [Benchmark(Description = "Baubit Generator GetNext()")]
    public Guid Baubit_Generator_GetNext()
    {
        return _generator.GetNext();
    }

    [Benchmark(Description = "Baubit Generator GetNext(timestamp)")]
    public Guid Baubit_Generator_GetNext_WithTimestamp()
    {
        return _generator.GetNext(_fixedTimestamp);
    }

    // ---- FastGuid comparison ----

    [Benchmark(Description = "FastGuid NewGuid()")]
    public Guid FastGuid_NewGuid()
    {
        return FastGuid.NewGuid();
    }

    [Benchmark(Description = "FastGuid NewSqlServerGuid()")]
    public Guid FastGuid_NewSqlServerGuid()
    {
        return FastGuid.NewSqlServerGuid();
    }

    [Benchmark(Description = "FastGuid NewPostgreSqlGuid()")]
    public Guid FastGuid_NewPostgreSqlGuid()
    {
        return FastGuid.NewPostgreSqlGuid();
    }
}
