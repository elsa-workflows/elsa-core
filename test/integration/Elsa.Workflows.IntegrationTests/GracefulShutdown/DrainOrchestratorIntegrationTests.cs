using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.GracefulShutdown;

/// <summary>
/// End-to-end DI integration tests: prove the orchestrator + signal + registries are correctly wired through
/// the full Elsa runtime registration. Drain-deadline-breach and workflow-cancellation scenarios are covered
/// by the unit tests in <c>DrainOrchestratorWaitTests</c>; here we only verify the production wiring resolves
/// correctly and a no-op drain returns the expected outcome.
/// </summary>
public class DrainOrchestratorIntegrationTests : IAsyncDisposable
{
    private readonly IServiceProvider _services;

    public DrainOrchestratorIntegrationTests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .ConfigureElsa(elsa => elsa
                .UseWorkflowRuntime(runtime => runtime
                    .ConfigureGracefulShutdown(options =>
                    {
                        options.DrainDeadline = TimeSpan.FromSeconds(2);
                        options.IngressPauseTimeout = TimeSpan.FromMilliseconds(200);
                    })))
            .Build();
    }

    [Test]
    [DisplayName("Full DI graph resolves IDrainOrchestrator, IQuiescenceSignal, IIngressSourceRegistry, IExecutionCycleRegistry")]
    public async Task ServicesResolveCleanly()
    {
        await Assert.That(_services.GetRequiredService<IDrainOrchestrator>()).IsNotNull();
        await Assert.That(_services.GetRequiredService<IQuiescenceSignal>()).IsNotNull();
        await Assert.That(_services.GetRequiredService<IIngressSourceRegistry>()).IsNotNull();
        await Assert.That(_services.GetRequiredService<IExecutionCycleRegistry>()).IsNotNull();
    }

    [Test]
    [DisplayName("Drain with no active execution cycles returns CompletedWithinDeadline")]
    public async Task EmptyGraphDrainsCleanly()
    {
        var orchestrator = _services.GetRequiredService<IDrainOrchestrator>();
        var signal = _services.GetRequiredService<IQuiescenceSignal>();

        var outcome = await orchestrator.DrainAsync(DrainTrigger.HostStopSignal);

        await Assert.That(outcome.OverallResult).IsEqualTo(DrainResult.CompletedWithinDeadline);
        // The default registration includes the internal bookmark-queue-worker as an ingress source for diagnostic
        // visibility — it is paused as a no-op. We assert that all sources reach a terminal "paused" state.
        foreach (var source in outcome.Sources)
            await Assert.That(source.State is IngressSourceState.Paused or IngressSourceState.PauseFailed).IsTrue();
        await Assert.That(outcome.ExecutionCyclesForceCancelledCount).IsEqualTo(0);
        await Assert.That(signal.CurrentState.Reason.HasFlag(QuiescenceReason.Drain)).IsTrue().Because("Drain flag should be set after drain.");
    }

    [Test]
    [DisplayName("Quiescence signal in the production DI graph reads as accepting new work at startup")]
    public async Task QuiescenceStartsAccepting()
    {
        var signal = _services.GetRequiredService<IQuiescenceSignal>();
        await Assert.That(signal.IsAcceptingNewWork).IsTrue();
        await Assert.That(signal.CurrentState.Reason).IsEqualTo(QuiescenceReason.None);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
