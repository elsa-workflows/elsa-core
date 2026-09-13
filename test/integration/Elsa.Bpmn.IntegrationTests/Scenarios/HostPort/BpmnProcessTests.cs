using Elsa.Common.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.Models;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;

/// <summary>
/// The BPMN processes the host port is exercised against, end to end through <c>IWorkflowRunner</c>.
/// </summary>
public class BpmnProcessTests : IAsyncDisposable
{
    private readonly BpmnTestHost _host = new(TestContext.Current!.Output.StandardOutput);

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Test]
    [DisplayName("An interrupting timer boundary event tears its host down and routes the boundary path")]
    public async Task InterruptingTimerBoundary_TearsTheHostDownAndRoutesTheBoundaryPath()
    {
        // Arrange
        await _host.RunAsync(BpmnTestProcesses.InterruptingTimerBoundary(_host.Log));

        // Act: the timer fires while the task is still running.
        var result = await _host.FinishWorkAsync("timeout");

        // Assert
        await Assert.That(_host.Log.Entries).Contains("cancelled:task");

        await Assert.That(_host.Log.Entries).Contains("executed:onTimeout");

        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

    }

    [Test]
    [DisplayName("An escalation out of an embedded subprocess is caught by a non-interrupting boundary event, and the subprocess keeps running")]
    public async Task EscalationOutOfSubprocess_IsCaughtWithoutInterruptingTheSubprocess()
    {
        // This is the nested-scope case. It only passes when the scope keys its work on the child activity execution
        // context rather than on ActivityExecutionContext.Tag, because a nested scope's tag is rewritten by its own
        // children's completion callbacks: the parent would be looking for a tag the child no longer wears.

        // Arrange
        await _host.RunAsync(BpmnTestProcesses.EscalationOutOfSubprocess(_host.Log));

        // Act: the subprocess reaches its escalation throw event.
        await _host.FinishWorkAsync("subWork");

        // Assert: the escalation crossed the scope boundary and the boundary path ran...
        await Assert.That(_host.Log.Entries).Contains("executed:notify");


        // ...while the subprocess carried on past the throw event rather than being torn down.
        await Assert.That(_host.Log.Entries).Contains("executed:subMore");

        await Assert.That(_host.Log.Entries).DoesNotContain("cancelled:subMore");


        // And the subprocess still completes normally, so the main path continues.
        var result = await _host.FinishWorkAsync("subMore");

        await Assert.That(_host.Log.Entries).Contains("executed:after");

        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

    }

    [Test]
    [DisplayName("A parallel split and join fires the join exactly once, after both branches")]
    public async Task ParallelSplitAndJoin_FiresTheJoinOnce()
    {
        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.ParallelSplitAndJoin(_host.Log));

        // Assert
        await Assert.That(_host.Log.Occurrences("executed:after")).IsEqualTo(1);

        await Assert.That(_host.Log.PositionOf("executed:left") < _host.Log.PositionOf("executed:after")).IsTrue();

        await Assert.That(_host.Log.PositionOf("executed:right") < _host.Log.PositionOf("executed:after")).IsTrue();

        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

    }

    [Test]
    [DisplayName("A caught error routes the boundary path and is not an incident")]
    public async Task ErrorBoundaryCaught_ContinuesDownTheBoundaryPath()
    {
        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.ErrorBoundaryCaught(_host.Log), typeof(FaultStrategy));

        // Assert: the disposition was Caught, so the scope claimed the fault and terminalized the failed work itself.
        await Assert.That(_host.Log.Entries).Contains("executed:recover");

        await Assert.That(StatusOf(result, "risky")).IsEqualTo(ActivityStatus.Canceled);


        // A fault a container claimed is not an incident: the middleware recovered the bookkeeping because
        // propagation was stopped.
        await Assert.That(result.WorkflowState.Incidents).IsEmpty();

        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);

        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

    }

    [Test]
    [DisplayName("An error nothing in a subprocess catches propagates to a boundary event on the subprocess")]
    public async Task ErrorPropagatedOutOfSubprocess_IsCaughtByTheEnclosingScope()
    {
        // The nested scope reports Propagated and leaves the signal alone, so it reaches the enclosing scope, which
        // resolves it to *its* failing unit of work — the subprocess — rather than to the activity that threw.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.ErrorPropagatedOutOfSubprocess(_host.Log), typeof(FaultStrategy));

        // Assert
        await Assert.That(_host.Log.Entries).Contains("executed:subRecover");

        await Assert.That(_host.Log.Entries).DoesNotContain("executed:after");


        // The whole failing unit of work is terminal, not just the activity that threw.
        await Assert.That(StatusOf(result, "sub")).IsEqualTo(ActivityStatus.Canceled);

        await Assert.That(StatusOf(result, "subRisky")).IsEqualTo(ActivityStatus.Canceled);


        await Assert.That(result.WorkflowState.Incidents).IsEmpty();

        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

    }

    [Test]
    [DisplayName("A collection-mode multi-instance runs one instance per item of a container-scoped variable")]
    public async Task CollectionMultiInstance_ReadsTheScopeVariable()
    {
        // Two things at once, and both fail loudly rather than quietly. If the host did not declare ScopeVariables,
        // BpmnGraph.Build would refuse the definition outright. If it declared the capability but its reader could
        // not answer, the interpreter would resolve an absent or null collection to an empty loop: "each" would never
        // run, "after" would run anyway, and the workflow would finish looking perfectly healthy.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.CollectionMultiInstanceTask(_host.Log));

        // Assert
        await Assert.That(_host.Log.Occurrences("executed:each")).IsEqualTo(3);

        await Assert.That(_host.Log.Occurrences("executed:after")).IsEqualTo(1);

        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

    }

    [Test]
    [DisplayName("A nested scope completing with a non-Done outcome reaches its parent's completion callback with that outcome intact")]
    public async Task CancelledTransactionSubprocess_DeliversTheCancelledOutcomeToTheEnclosingScope()
    {
        // The case to write first. A nested scope's outcome is not decoration: the transaction completes 'Cancelled'
        // instead of 'Done', and the enclosing scope routes the cancel boundary event only because that name arrives
        // intact on the completion callback. Drop it anywhere on the way — at CompleteActivityAsync, or when the
        // callback turns the result back into outcome names — and the parent takes the ordinary sequence flow and
        // finishes successfully, which is the shape of failure this test exists to catch.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.CancelledTransactionSubprocess(_host.Log));

        // Assert: the cancellation path ran...
        await Assert.That(_host.Log.Entries).Contains("executed:unwind");


        // ...and the ordinary path the 'Done' outcome would have taken did not.
        await Assert.That(_host.Log.Entries).DoesNotContain("executed:after");


        await Assert.That(result.WorkflowState.Incidents).IsEmpty();

        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

    }

    [Test]
    [DisplayName("An uncaught error propagates and is left to the incident strategy")]
    public async Task UncaughtError_PropagatesToTheIncidentStrategy()
    {
        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.UncaughtError(_host.Log), typeof(FaultStrategy));

        // Assert: the disposition was Propagated, so the scope did not stop propagation and the incident strategy ran
        // exactly as it would with no handler present.
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);

        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Faulted);


        var incident = (await Assert.That(result.WorkflowState.Incidents).HasSingleItem())!;

        await Assert.That(incident.ActivityId).IsEqualTo("risky");

        await Assert.That(StatusOf(result, "risky")).IsEqualTo(ActivityStatus.Faulted);

    }

    private static ActivityStatus? StatusOf(RunWorkflowResult result, string activityId) =>
        result.Journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity.Id == activityId)?.Status;
}
