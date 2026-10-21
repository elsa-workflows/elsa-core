using Elsa.Common.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.Models;
using Xunit.Abstractions;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;

/// <summary>
/// The BPMN processes the host port is exercised against, end to end through <c>IWorkflowRunner</c>.
/// </summary>
public class BpmnProcessTests(ITestOutputHelper testOutputHelper)
{
    private readonly BpmnTestHost _host = new(testOutputHelper);

    [Fact(DisplayName = "An interrupting timer boundary event tears its host down and routes the boundary path")]
    public async Task InterruptingTimerBoundary_TearsTheHostDownAndRoutesTheBoundaryPath()
    {
        // Arrange
        await _host.RunAsync(BpmnTestProcesses.InterruptingTimerBoundary(_host.Log));

        // Act: the timer fires while the task is still running.
        var result = await _host.FinishWorkAsync("timeout");

        // Assert
        Assert.Contains("cancelled:task", _host.Log.Entries);
        Assert.Contains("executed:onTimeout", _host.Log.Entries);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "An escalation out of an embedded subprocess is caught by a non-interrupting boundary event, and the subprocess keeps running")]
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
        Assert.Contains("executed:notify", _host.Log.Entries);

        // ...while the subprocess carried on past the throw event rather than being torn down.
        Assert.Contains("executed:subMore", _host.Log.Entries);
        Assert.DoesNotContain("cancelled:subMore", _host.Log.Entries);

        // And the subprocess still completes normally, so the main path continues.
        var result = await _host.FinishWorkAsync("subMore");

        Assert.Contains("executed:after", _host.Log.Entries);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A parallel split and join fires the join exactly once, after both branches")]
    public async Task ParallelSplitAndJoin_FiresTheJoinOnce()
    {
        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.ParallelSplitAndJoin(_host.Log));

        // Assert
        Assert.Equal(1, _host.Log.Occurrences("executed:after"));
        Assert.True(_host.Log.PositionOf("executed:left") < _host.Log.PositionOf("executed:after"));
        Assert.True(_host.Log.PositionOf("executed:right") < _host.Log.PositionOf("executed:after"));
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A caught error routes the boundary path and is not an incident")]
    public async Task ErrorBoundaryCaught_ContinuesDownTheBoundaryPath()
    {
        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.ErrorBoundaryCaught(_host.Log), typeof(FaultStrategy));

        // Assert: the disposition was Caught, so the scope claimed the fault and terminalized the failed work itself.
        Assert.Contains("executed:recover", _host.Log.Entries);
        Assert.Equal(ActivityStatus.Canceled, StatusOf(result, "risky"));

        // A fault a container claimed is not an incident: the middleware recovered the bookkeeping because
        // propagation was stopped.
        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "An error nothing in a subprocess catches propagates to a boundary event on the subprocess")]
    public async Task ErrorPropagatedOutOfSubprocess_IsCaughtByTheEnclosingScope()
    {
        // The nested scope reports Propagated and leaves the signal alone, so it reaches the enclosing scope, which
        // resolves it to *its* failing unit of work — the subprocess — rather than to the activity that threw.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.ErrorPropagatedOutOfSubprocess(_host.Log), typeof(FaultStrategy));

        // Assert
        Assert.Contains("executed:subRecover", _host.Log.Entries);
        Assert.DoesNotContain("executed:after", _host.Log.Entries);

        // The whole failing unit of work is terminal, not just the activity that threw.
        Assert.Equal(ActivityStatus.Canceled, StatusOf(result, "sub"));
        Assert.Equal(ActivityStatus.Canceled, StatusOf(result, "subRisky"));

        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "An error boundary inside a subprocess catches a failure there, however the scopes' tokens are numbered")]
    public async Task ErrorBoundaryInsideSubprocess_CatchesTheFailure()
    {
        // valence-works/bpmn#32, reported against Elsa as #8834: the failure was resolved against the root's token,
        // so the boundary inside the body caught it only when the two scopes' token ids happened to coincide.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.ErrorBoundaryInsideSubprocess(_host.Log), typeof(FaultStrategy));

        // Assert
        Assert.Equal(["executed:first", "executed:second", "executed:subRisky", "cancelled:subRisky", "executed:subRecover", "executed:after"], _host.Log.Entries);
        Assert.Equal(ActivityStatus.Canceled, StatusOf(result, "subRisky"));
        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A failing instance of a parallel multi-instance task is told apart from its live sibling, and its error boundary catches it")]
    public async Task FailingParallelMultiInstance_IsPinnedOnItsOwnInstance()
    {
        // #8834: two instances of one binding are live when the first fails. Bpmn.Semantics 0.4.0 refuses a fault it
        // cannot pin on exactly one of them, so the host must name the failed instance by its iteration and token id.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.ParallelMultiInstanceTaskWithErrorBoundary(_host.Log), typeof(FaultStrategy));

        // Assert
        Assert.Equal(1, _host.Log.Occurrences("executed:recover"));
        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A coded fault is caught by the error boundary that declares its code, ahead of the catch-all")]
    public async Task CodedFault_IsCaughtByTheBoundaryForItsCode()
    {
        // The FaultException's code is the BPMN error the work raised; a catcher that declares it wins over a catch-all.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.CodedErrorBoundaries(_host.Log, code: "PAY-01", boundaryCode: "PAY-01", withCatchAll: true), typeof(FaultStrategy));

        // Assert
        Assert.Contains("executed:handleCoded", _host.Log.Entries);
        Assert.DoesNotContain("executed:handleAny", _host.Log.Entries);
        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A coded fault passes over an error boundary for another code and reaches the catch-all")]
    public async Task CodedFault_PassesOverABoundaryForAnotherCode()
    {
        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.CodedErrorBoundaries(_host.Log, code: "PAY-02", boundaryCode: "PAY-01", withCatchAll: true), typeof(FaultStrategy));

        // Assert
        Assert.Contains("executed:handleAny", _host.Log.Entries);
        Assert.DoesNotContain("executed:handleCoded", _host.Log.Entries);
        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A fault with no code is not caught by an error boundary that declares one")]
    public async Task UncodedFault_IsNotCaughtByACodedBoundary()
    {
        // An ordinary exception names no BPMN error, so only a catch-all catches it; with none, it is an incident.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.CodedErrorBoundaries(_host.Log, code: null, boundaryCode: "PAY-01", withCatchAll: false), typeof(FaultStrategy));

        // Assert
        Assert.DoesNotContain("executed:handleCoded", _host.Log.Entries);
        Assert.Single(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Faulted, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "An error end event nothing in a subprocess catches is caught by the boundary on the subprocess that declares its code")]
    public async Task ErrorEndEventInASubprocess_IsCaughtByItsCodeInTheEnclosingScope()
    {
        // The body faults with bpmn.error.thrown and the error's code; the enclosing scope reports that code as the
        // error its subprocess raised.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.ErrorEndEventCaughtByTheEnclosingScope(_host.Log, "SHIP-01"), typeof(FaultStrategy));

        // Assert
        Assert.Contains("executed:subRecover", _host.Log.Entries);
        Assert.DoesNotContain("executed:after", _host.Log.Entries);
        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A subprocess body that cannot run is refused before the process runs anything")]
    public async Task SubprocessBodyThatCannotRun_IsRefusedBeforeAnythingRuns()
    {
        // The body's graph used to be built only when a token reached the subprocess, after "first" had already run.
        // The root now builds every body it can reach before it starts.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.SubprocessWithABodyThatCannotRun(_host.Log), typeof(FaultStrategy));

        // Assert
        Assert.Empty(_host.Log.Entries);
        Assert.Equal(WorkflowSubStatus.Faulted, result.WorkflowState.SubStatus);
        var incident = Assert.Single(result.WorkflowState.Incidents);
        Assert.Contains("loopA", incident.Exception!.Message);
    }

    [Fact(DisplayName = "A collection-mode multi-instance runs one instance per item of a container-scoped variable")]
    public async Task CollectionMultiInstance_ReadsTheScopeVariable()
    {
        // Two things at once, and both fail loudly rather than quietly. If the host did not declare ScopeVariables,
        // BpmnGraph.Build would refuse the definition outright. If it declared the capability but its reader could
        // not answer, the interpreter would resolve an absent or null collection to an empty loop: "each" would never
        // run, "after" would run anyway, and the workflow would finish looking perfectly healthy.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.CollectionMultiInstanceTask(_host.Log));

        // Assert
        Assert.Equal(3, _host.Log.Occurrences("executed:each"));
        Assert.Equal(1, _host.Log.Occurrences("executed:after"));
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A nested scope completing with a non-Done outcome reaches its parent's completion callback with that outcome intact")]
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
        Assert.Contains("executed:unwind", _host.Log.Entries);

        // ...and the ordinary path the 'Done' outcome would have taken did not.
        Assert.DoesNotContain("executed:after", _host.Log.Entries);

        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "An uncaught error propagates and is left to the incident strategy")]
    public async Task UncaughtError_PropagatesToTheIncidentStrategy()
    {
        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.UncaughtError(_host.Log), typeof(FaultStrategy));

        // Assert: the disposition was Propagated, so the scope did not stop propagation and the incident strategy ran
        // exactly as it would with no handler present.
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Equal(WorkflowSubStatus.Faulted, result.WorkflowState.SubStatus);

        var incident = Assert.Single(result.WorkflowState.Incidents);
        Assert.Equal("risky", incident.ActivityId);
        Assert.Equal(ActivityStatus.Faulted, StatusOf(result, "risky"));
    }

    private static ActivityStatus? StatusOf(RunWorkflowResult result, string activityId) =>
        result.Journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity.Id == activityId)?.Status;
}
