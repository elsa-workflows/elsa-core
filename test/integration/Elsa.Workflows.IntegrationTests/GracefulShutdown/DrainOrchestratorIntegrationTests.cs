using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.GracefulShutdown;

/// <summary>
/// End-to-end DI integration tests: prove the orchestrator + signal + registries are correctly wired through
/// the full Elsa runtime registration. Drain-deadline-breach and workflow-cancellation scenarios are covered
/// by the unit tests in <c>DrainOrchestratorWaitTests</c>; here we only verify the production wiring resolves
/// correctly and a no-op drain returns the expected outcome.
/// </summary>
public class DrainOrchestratorIntegrationTests
{
    private readonly IServiceProvider _services;

    public DrainOrchestratorIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa
                .UseWorkflowRuntime(runtime => runtime
                    .ConfigureGracefulShutdown(options =>
                    {
                        options.DrainDeadline = TimeSpan.FromSeconds(2);
                        options.IngressPauseTimeout = TimeSpan.FromMilliseconds(200);
                    })))
            .Build();
    }

    [Fact(DisplayName = "Full DI graph resolves IDrainOrchestrator, IQuiescenceSignal, IIngressSourceRegistry, IExecutionCycleRegistry")]
    public void ServicesResolveCleanly()
    {
        Assert.NotNull(_services.GetRequiredService<IDrainOrchestrator>());
        Assert.NotNull(_services.GetRequiredService<IQuiescenceSignal>());
        Assert.NotNull(_services.GetRequiredService<IIngressSourceRegistry>());
        Assert.NotNull(_services.GetRequiredService<IExecutionCycleRegistry>());
    }

    [Fact(DisplayName = "Drain with no active execution cycles returns CompletedWithinDeadline")]
    public async Task EmptyGraphDrainsCleanly()
    {
        var orchestrator = _services.GetRequiredService<IDrainOrchestrator>();
        var signal = _services.GetRequiredService<IQuiescenceSignal>();

        var outcome = await orchestrator.DrainAsync(DrainTrigger.HostStopSignal);

        Assert.Equal(DrainResult.CompletedWithinDeadline, outcome.OverallResult);
        // The default registration includes the internal bookmark-queue-worker as an ingress source for diagnostic
        // visibility — it is paused as a no-op. We assert that all sources reach a terminal "paused" state.
        Assert.All(outcome.Sources, s => Assert.True(s.State == IngressSourceState.Paused || s.State == IngressSourceState.PauseFailed));
        Assert.Equal(0, outcome.ExecutionCyclesForceCancelledCount);
        Assert.True(signal.CurrentState.Reason.HasFlag(QuiescenceReason.Drain), "Drain flag should be set after drain.");
    }

    [Fact(DisplayName = "Quiescence signal in the production DI graph reads as accepting new work at startup")]
    public void QuiescenceStartsAccepting()
    {
        var signal = _services.GetRequiredService<IQuiescenceSignal>();
        Assert.True(signal.IsAcceptingNewWork);
        Assert.Equal(QuiescenceReason.None, signal.CurrentState.Reason);
    }
}
