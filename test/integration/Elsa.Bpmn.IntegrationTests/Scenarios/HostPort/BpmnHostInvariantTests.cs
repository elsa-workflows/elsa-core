using Elsa.Common.Models;
using Elsa.Workflows;
using Elsa.Workflows.IncidentStrategies;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;

/// <summary>
/// The invariants the interpreter relies on its host to uphold. Each of these is a rule a host can break while every
/// process still appears to run, which is exactly why they are pinned separately from the processes.
/// </summary>
public class BpmnHostInvariantTests : IAsyncDisposable
{
    private readonly BpmnTestHost _host = new(TestContext.Current!.Output.StandardOutput);

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Test]
    [DisplayName("Commands are applied in the order returned")]
    public async Task Commands_AreAppliedInTheOrderReturned()
    {
        // An interrupting boundary event returns the boundary path's StartWork *before* the teardown that retires the
        // host it interrupted. The interrupted task records what was in the scheduler at the moment it was torn down,
        // so applying the teardown first is visible: the boundary path would not be there yet.

        // Arrange
        await _host.RunAsync(BpmnTestProcesses.InterruptingTimerBoundary(_host.Log));

        // Act
        await _host.FinishWorkAsync("timeout");

        // Assert
        await Assert.That(_host.Log.Snapshot("scheduled@cancel:task")).Contains("onTimeout");

    }

    [Test]
    [DisplayName("Completed work is gone from LiveWork")]
    public async Task CompletedWork_IsRemovedFromLiveWork()
    {
        // The next activity the evaluation schedules reads the scope's ledger, which is the sole source of
        // BpmnHostSnapshot.LiveWork. Both the completed listener and the torn-down host must be gone from it:
        // completion is terminal, and leaving it lets a later teardown resolve the handle of work that already
        // finished.
        //
        // The applier removes the record before asking the interpreter, which is what the port requires. That stricter
        // ordering is not asserted here because it is not observable for these constructs: the interpreter reads
        // LiveWork only to resolve teardown handles by (binding ref, iteration id), and none of the processes in scope
        // tears down a slot a just-completed unit of work shares. It becomes observable with multi-instance work and
        // re-armed scope listeners, which arrive with the issues that add them -- see
        // BpmnEventSubprocessTests.NonInterruptingListener_ReArmsWithoutCollidingWithTheSlotItJustVacated, which is
        // where the ordering is pinned.

        // Arrange
        await _host.RunAsync(BpmnTestProcesses.InterruptingTimerBoundary(_host.Log));

        // Act
        await _host.FinishWorkAsync("timeout");

        // Assert
        await Assert.That(_host.Log.Snapshot("liveWork@onTimeout")).IsEquivalentTo([BpmnTestProcesses.BindingRef("onTimeout")], TUnit.Assertions.Enums.CollectionOrdering.Matching);

    }

    [Test]
    [DisplayName("Faulted work is gone from LiveWork")]
    public async Task FaultedWork_IsRemovedFromLiveWork()
    {
        // Act
        await _host.RunAsync(BpmnTestProcesses.ErrorBoundaryCaught(_host.Log), typeof(FaultStrategy));

        // Assert: only the boundary path's work is live by the time it runs; the failed work is not.
        await Assert.That(_host.Log.Snapshot("liveWork@recover")).IsEquivalentTo([BpmnTestProcesses.BindingRef("recover")], TUnit.Assertions.Enums.CollectionOrdering.Matching);

    }

    [Test]
    [DisplayName("Signalling work is still in LiveWork when the interpreter is asked about the signal")]
    public async Task SignallingWork_StaysInLiveWork()
    {
        // A signal is not terminal: the escalating subprocess keeps running. Removing it would make the interpreter
        // believe the boundary event's host had already gone, so the escalation would be recorded as late and the
        // boundary path below would never be scheduled at all.

        // Arrange
        await _host.RunAsync(BpmnTestProcesses.EscalationOutOfSubprocess(_host.Log));

        // Act
        await _host.FinishWorkAsync("subWork");

        // Assert: the boundary path ran, and the subprocess that raised the escalation was still live when it did.
        await Assert.That(_host.Log.Entries).Contains("executed:notify");

        await Assert.That(_host.Log.Snapshot("liveWork@notify")).Contains(BpmnTestProcesses.BindingRef("sub"));

    }

    [Test]
    [DisplayName("A parent evaluation raised mid-apply is queued and drained, not recursed")]
    public async Task ParentEvaluationRaisedMidApply_IsQueuedNotRecursed()
    {
        // The subprocess's evaluation returns [SignalEnclosingScope, StartWork(subMore)]. Delivering the signal reaches
        // the parent scope synchronously, and the parent's own evaluation must wait until the subprocess has finished
        // applying its command list rather than running on top of it.
        //
        // The observable difference is the order in which the two branches' work is scheduled. Queued, the subprocess
        // schedules subMore first and the parent schedules notify second; recursing swaps them, because the parent runs
        // between the signal and the StartWork that follows it. Elsa's scheduler is FIFO, so scheduling order is also
        // execution order.

        // Arrange
        await _host.RunAsync(BpmnTestProcesses.EscalationOutOfSubprocess(_host.Log));

        // Act
        await _host.FinishWorkAsync("subWork");

        // Assert
        await Assert.That(_host.Log.PositionOf("executed:subMore") < _host.Log.PositionOf("executed:notify"))
            .IsTrue()
            .Because($"Expected the subprocess to finish applying its command list before the parent's escalation path was scheduled, but the log was: {string.Join(", ", _host.Log.Entries)}.");
    }

    [Test]
    [DisplayName("Concurrent instances of one binding are told apart by their iteration id")]
    public async Task ConcurrentInstancesOfOneBinding_AreToldApartByIterationId()
    {
        // The interpreter re-finds a parked token from (binding ref, iteration id) alone, so a scope may never hold two
        // live units of work under one such pair. Both instances of this task share a binding ref and one activity, so
        // nothing but the iteration id — and, on the host side, the child activity execution — separates them.

        // Arrange
        await _host.RunAsync(BpmnTestProcesses.ParallelMultiInstanceTask(_host.Log));

        // Assert: two live instances, distinguished only by the iteration id.
        var liveWork = _host.LiveWorkOf("scope");
        await Assert.That(liveWork.Count).IsEqualTo(2);

        foreach (var work in liveWork)
            await Assert.That(work.BindingRef).IsEqualTo(BpmnTestProcesses.BindingRef("each"));
        await Assert.That(liveWork.Select(work => work.IterationId).Distinct().Count()).IsEqualTo(2);

        await Assert.That(liveWork).DoesNotContain(work => work.IterationId is null);


        // Act: finish both instances.
        await _host.FinishWorkAsync("each");
        var result = await _host.FinishWorkAsync("each");

        // Assert
        await Assert.That(_host.Log.Occurrences("executed:each")).IsEqualTo(2);

        await Assert.That(_host.Log.Occurrences("executed:after")).IsEqualTo(1);

        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

    }

    [Test]
    [DisplayName("A scope completes because the interpreter said so")]
    public async Task Scope_CompletesOnTheInterpretersContinuation()
    {
        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.LinearTask(_host.Log));

        // Assert
        await Assert.That(result.Journal.ActivityExecutionContexts.First(x => x.Activity.Id == "scope").Status).IsEqualTo(ActivityStatus.Completed);

        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

    }
}
