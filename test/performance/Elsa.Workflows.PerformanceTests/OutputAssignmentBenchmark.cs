using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.PerformanceTests;

/// <summary>
/// Guards the allocation-sensitive output assignment path when no converter is configured.
/// </summary>
[Config(typeof(OutputAssignmentBenchmarkConfig))]
[MemoryDiagnoser]
public class OutputAssignmentBenchmark
{
    private ActivityExecutionContext _activityExecutionContext = null!;
    private Output<int> _output = null!;
    private ServiceProvider _serviceProvider = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        var services = new ServiceCollection();
        services.AddElsa();
        _serviceProvider = services.BuildServiceProvider();

        var activity = new WriteLine("benchmark");
        var registry = _serviceProvider.GetRequiredService<IActivityRegistry>();
        await registry.RegisterAsync(activity.GetType());

        var graphBuilder = _serviceProvider.GetRequiredService<IWorkflowGraphBuilder>();
        var workflow = Workflow.FromActivity(activity);
        var graph = await graphBuilder.BuildAsync(workflow);
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(
            _serviceProvider,
            graph,
            "output-assignment-benchmark",
            CancellationToken.None);

        _activityExecutionContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(activity);
        _output = new(new MemoryBlockReference("benchmark-output"));
    }

    [Benchmark(Baseline = true)]
    public void LegacyEquivalentAssignment() => LegacyEquivalentAssignmentCore(_output, 42, nameof(_output));

    private void LegacyEquivalentAssignmentCore(Output? output, object? value, string? outputName)
    {
        _activityExecutionContext.ExpressionExecutionContext.Set(output, value);
        RecordActivityOutput(
            _activityExecutionContext.WorkflowExecutionContext,
            _activityExecutionContext,
            outputName,
            value);
    }

    [Benchmark]
    public void NoConverterAssignment() => _activityExecutionContext.Set(_output, 42);

    [IterationSetup]
    public void IterationSetup()
    {
        var register = _activityExecutionContext.WorkflowExecutionContext.GetActivityOutputRegister();
        GetRecordsByActivityIdAndOutputName(register).Clear();
        GetRecordsByActivityInstanceIdAndOutputName(register).Clear();
    }

    [GlobalCleanup]
    public void GlobalCleanup() => _serviceProvider.Dispose();

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "RecordActivityOutput")]
    private static extern void RecordActivityOutput(
        WorkflowExecutionContext workflowExecutionContext,
        ActivityExecutionContext activityExecutionContext,
        string? outputName,
        object? value);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_recordsByActivityIdAndOutputName")]
    private static extern ref Dictionary<string, List<ActivityOutputRecord>> GetRecordsByActivityIdAndOutputName(ActivityOutputRegister register);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_recordsByActivityInstanceIdAndOutputName")]
    private static extern ref Dictionary<string, ActivityOutputRecord> GetRecordsByActivityInstanceIdAndOutputName(ActivityOutputRegister register);
}

public sealed class OutputAssignmentBenchmarkConfig : Config
{
    public OutputAssignmentBenchmarkConfig()
    {
        AddJob(Job.Default
            .WithLaunchCount(1)
            .WithWarmupCount(5)
            .WithIterationCount(10)
            .WithInvocationCount(65536)
            .WithUnrollFactor(1));
    }
}
