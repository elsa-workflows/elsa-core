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
