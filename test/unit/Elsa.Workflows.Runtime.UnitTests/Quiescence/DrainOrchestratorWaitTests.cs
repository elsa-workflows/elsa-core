using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.HostedServices;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Quiescence;

public class DrainOrchestratorWaitTests : DrainOrchestratorTestsBase
{
    [Test]
    [DisplayName("Wait returns CompletedWithinDeadline when execution cycle count reaches zero before deadline")]
    public async Task ExecutionCyclesCompleteBeforeDeadline()
    {
        // Simulate two active execution cycles that drain to zero on the second poll iteration.
        var calls = 0;
        ExecutionCycleRegistry.ActiveCount.Returns(_ => calls++ switch { 0 => 2, 1 => 0, _ => 0 });

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.HostStopSignal);

        await Assert.That(outcome.OverallResult).IsEqualTo(DrainResult.CompletedWithinDeadline);
        await Assert.That(outcome.ExecutionCyclesForceCancelledCount).IsEqualTo(0);
    }

    [Test]
    [DisplayName("Operator force trigger always reports Forced result and force-cancels live execution cycles")]
    public async Task ForceTriggerCancelsAllCycles()
    {
        using var handle = new ExecutionCycleHandle(Guid.NewGuid(), "instance-1", ingressSourceName: "http.trigger", startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None);
        ExecutionCycleRegistry.ActiveCount.Returns(1);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(new WorkflowInstance
            {
                Id = "instance-1",
                DefinitionId = "def-1",
                DefinitionVersionId = "ver-1",
                Version = 1,
                IsExecuting = true,
            }));

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.OperatorForce);

        await Assert.That(outcome.OverallResult).IsEqualTo(DrainResult.Forced);
        await Assert.That(outcome.ExecutionCyclesForceCancelledCount).IsEqualTo(1);
        await Assert.That(outcome.ForceCancelledInstanceIds).Contains("instance-1");
        await Assert.That(handle.CancellationToken.IsCancellationRequested).IsTrue();
        await InstanceStore.Received(1).SaveAsync(Arg.Is<WorkflowInstance>(i => i.SubStatus == WorkflowSubStatus.Interrupted && !i.IsExecuting), Arg.Any<CancellationToken>());
        await LogStore.Received(1).AddAsync(Arg.Is<Entities.WorkflowExecutionLogRecord>(r => r.EventName == WorkflowInterruptedPayload.WorkflowInterruptedEventName), Arg.Any<CancellationToken>());
    }

    [Test]
    [DisplayName("Persistence failure during drain produces Reason=PersistenceFailure in payload")]
    public async Task PersistenceFailureRecordsReason()
    {
        using var handle = new ExecutionCycleHandle(Guid.NewGuid(), "instance-2", ingressSourceName: null, startedAt: DateTimeOffset.UtcNow, linkedToken: CancellationToken.None);
        ExecutionCycleRegistry.ActiveCount.Returns(1);
        ExecutionCycleRegistry.ListActiveCycles().Returns(new[] { handle });
        InstanceStore.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<WorkflowInstance?>(new WorkflowInstance
            {
                Id = "instance-2",
                DefinitionId = "def-2",
                DefinitionVersionId = "ver-2",
                Version = 1,
                IsExecuting = true,
            }));
        InstanceStore.SaveAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("db unavailable"));

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.OperatorForce);

        await Assert.That(outcome.OverallResult).IsEqualTo(DrainResult.Forced);
        await LogStore.Received(1).AddAsync(
            Arg.Is<Entities.WorkflowExecutionLogRecord>(r =>
                r.EventName == WorkflowInterruptedPayload.WorkflowInterruptedEventName
                && ((WorkflowInterruptedPayload)r.Payload!).Reason == WorkflowInterruptedPayload.ReasonPersistenceFailure),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [DisplayName("Second non-force drain in same generation throws InvalidOperationException")]
    public async Task SecondNonForceDrainThrows()
    {
        var sut = BuildSut();
        await sut.DrainAsync(DrainTrigger.HostStopSignal);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await sut.DrainAsync(DrainTrigger.HostStopSignal));
    }

    [Test]
    [DisplayName("Operator force after a completed drain returns the previous outcome with WasCached=true")]
    public async Task OperatorForceAfterPreviousReturnsCachedOutcome()
    {
        var sut = BuildSut();
        var first = await sut.DrainAsync(DrainTrigger.HostStopSignal);
        var second = await sut.DrainAsync(DrainTrigger.OperatorForce);

        await Assert.That(first.WasCached).IsFalse().Because("First (fresh) drain must not be flagged as cached.");
        await Assert.That(second.WasCached).IsTrue().Because("Second (force-after-completed) drain must be flagged as cached so the admin endpoint skips audit publishing.");
        // Same payload modulo the WasCached flag.
        await Assert.That(second).IsEqualTo(first with { WasCached = true });
    }

    [Test]
    [DisplayName("Host stop drain swallows ObjectDisposedException after shutdown cancellation")]
    public async Task HostStopDrainSwallowsObjectDisposedExceptionAfterCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var orchestrator = Substitute.For<IDrainOrchestrator>();
        orchestrator.DrainAsync(DrainTrigger.HostStopSignal, cts.Token).Returns(_ => ThrowObjectDisposedAsync());
        var hostedService = new DrainOrchestratorHostedService(orchestrator, Substitute.For<ILogger<DrainOrchestratorHostedService>>());

        await hostedService.StopAsync(cts.Token);
    }

    [Test]
    [DisplayName("Host stop drain propagates ObjectDisposedException before shutdown cancellation")]
    public async Task HostStopDrainPropagatesObjectDisposedExceptionBeforeCancellation()
    {
        var orchestrator = Substitute.For<IDrainOrchestrator>();
        orchestrator.DrainAsync(DrainTrigger.HostStopSignal, CancellationToken.None).Returns(_ => ThrowObjectDisposedAsync());
        var hostedService = new DrainOrchestratorHostedService(orchestrator, Substitute.For<ILogger<DrainOrchestratorHostedService>>());

        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => hostedService.StopAsync(CancellationToken.None));
    }

    private static async ValueTask<DrainOutcome> ThrowObjectDisposedAsync()
    {
        await Task.Yield();
        throw new ObjectDisposedException("drain dependency");
    }
}
