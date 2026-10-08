using BenchmarkDotNet.Attributes;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.PerformanceTests;

[Config(typeof(Config))]
public class ConsoleActivitiesBenchmark
{
    private WriteLine _writeLineWorkflow = null!;
    private IWorkflowRunner _workflowRunner = null!;
    private ServiceProvider _serviceProvider = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection();
        services.AddElsa();

        _serviceProvider = services.BuildServiceProvider();
        _workflowRunner = _serviceProvider.GetRequiredService<IWorkflowRunner>();
        
        _writeLineWorkflow = new("Hello, World!");
    }
    
    [Benchmark]
    public async Task WriteLine() => await _workflowRunner.RunAsync(_writeLineWorkflow);

    [GlobalCleanup]
    public void GlobalCleanup() => _serviceProvider.Dispose();
}