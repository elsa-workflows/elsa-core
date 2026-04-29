using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.GracefulShutdown;

/// <summary>
/// Verifies that the workflow execution pipeline registers a <see cref="ExecutionCycleHandle"/> with <see cref="IExecutionCycleRegistry"/>
/// for the lifetime of a single workflow execution cycle — the property the drain orchestrator depends on.
/// </summary>
public class ExecutionCycleTrackingMiddlewareTests
{
    private readonly IServiceProvider _services;
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IExecutionCycleRegistry _cycleRegistry;

    public ExecutionCycleTrackingMiddlewareTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseWorkflowRuntime())
            .Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
        _cycleRegistry = _services.GetRequiredService<IExecutionCycleRegistry>();
    }

    [Fact(DisplayName = "ActiveCount returns to zero after a workflow execution cycle completes")]
    public async Task ActiveCountReturnsToZero()
    {
        await _services.PopulateRegistriesAsync();

        Assert.Equal(0, _cycleRegistry.ActiveCount);

        var workflow = new TestWorkflow(builder => builder.Root = new WriteLine("hello"));
        await _workflowRunner.RunAsync(workflow);

        Assert.Equal(0, _cycleRegistry.ActiveCount);
    }

    [Fact(DisplayName = "ExecutionCycleRegistry tracks an active handle while the execution cycle is running")]
    public async Task ExecutionCycleRegistryTracksActiveCycle()
    {
        await _services.PopulateRegistriesAsync();

        var observedActiveCounts = new List<int>();
        var observingActivity = new ObservingActivity(_cycleRegistry, observedActiveCounts);
        var workflow = new TestWorkflow(builder => builder.Root = observingActivity);

        await _workflowRunner.RunAsync(workflow);

        // The activity executed inside the execution cycle — when it ran, the registry should have shown ≥ 1 active execution cycle.
        Assert.NotEmpty(observedActiveCounts);
        Assert.All(observedActiveCounts, count => Assert.True(count >= 1, $"Expected ≥ 1 active execution cycle during execution; saw {count}."));
        // After the execution cycle completes, count is back to zero.
        Assert.Equal(0, _cycleRegistry.ActiveCount);
    }

    /// <summary>An activity that records the execution cycle registry's active count when it executes.</summary>
    private sealed class ObservingActivity(IExecutionCycleRegistry registry, List<int> observed) : CodeActivity
    {
        protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            observed.Add(registry.ActiveCount);
            return ValueTask.CompletedTask;
        }
    }
}
