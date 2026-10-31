using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;

namespace Elsa.Workflows.PerformanceTests;

public class Config : ManualConfig
{
    public Config()
    {
        BuildTimeout = TimeSpan.FromMinutes(5);
        AddExporter(MarkdownExporter.GitHub);
        AddDiagnoser(MemoryDiagnoser.Default);
    }
}
