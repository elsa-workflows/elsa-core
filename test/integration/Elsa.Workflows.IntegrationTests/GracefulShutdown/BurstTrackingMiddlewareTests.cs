using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.GracefulShutdown;

/// <summary>
/// Verifies that the workflow execution pipeline registers a <see cref="BurstHandle"/> with <see cref="IBurstRegistry"/>
/// for the lifetime of a single workflow burst — the property the drain orchestrator depends on.
/// </summary>
public class BurstTrackingMiddlewareTests
{
    private readonly IServiceProvider _services;
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IBurstRegistry _burstRegistry;

    public BurstTrackingMiddlewareTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseWorkflowRuntime())
            .Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
        _burstRegistry = _services.GetRequiredService<IBurstRegistry>();
    }

    [Fact(DisplayName = "ActiveCount returns to zero after a workflow burst completes")]
    public async Task ActiveCountReturnsToZero()
    {
        await _services.PopulateRegistriesAsync();

        Assert.Equal(0, _burstRegistry.ActiveCount);

        var workflow = new TestWorkflow(builder => builder.Root = new WriteLine("hello"));
        await _workflowRunner.RunAsync(workflow);

        Assert.Equal(0, _burstRegistry.ActiveCount);
    }

    [Fact(DisplayName = "BurstRegistry tracks an active handle while the burst is running")]
    public async Task BurstRegistryTracksActiveBurst()
    {
        await _services.PopulateRegistriesAsync();

        var observedActiveCounts = new List<int>();
        var observingActivity = new ObservingActivity(_burstRegistry, observedActiveCounts);
        var workflow = new TestWorkflow(builder => builder.Root = observingActivity);

        await _workflowRunner.RunAsync(workflow);

        // The activity executed inside the burst — when it ran, the registry should have shown ≥ 1 active burst.
        Assert.NotEmpty(observedActiveCounts);
        Assert.All(observedActiveCounts, count => Assert.True(count >= 1, $"Expected ≥ 1 active burst during execution; saw {count}."));
        // After the burst completes, count is back to zero.
        Assert.Equal(0, _burstRegistry.ActiveCount);
    }

    /// <summary>An activity that records the burst registry's active count when it executes.</summary>
    private sealed class ObservingActivity(IBurstRegistry registry, List<int> observed) : CodeActivity
    {
        protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            observed.Add(registry.ActiveCount);
            return ValueTask.CompletedTask;
        }
    }
}
