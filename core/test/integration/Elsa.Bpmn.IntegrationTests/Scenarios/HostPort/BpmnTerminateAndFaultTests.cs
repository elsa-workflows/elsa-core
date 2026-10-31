using Elsa.Bpmn.Exceptions;
using Elsa.Bpmn.Hosting;
using Elsa.Workflows;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.Models;
using Xunit.Abstractions;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;

/// <summary>
/// A terminate end event or a behavior fault ends its BPMN scope at once, including when it is reached in the same
/// evaluation that also activated new work. That work is withdrawn rather than started, so nothing is left behind
/// for the process to wait on.
/// </summary>
public class BpmnTerminateAndFaultTests(ITestOutputHelper testOutputHelper)
{
    private readonly BpmnTestHost _host = new(testOutputHelper);

    [Fact(DisplayName = "A terminate reached in the evaluation that also starts work finishes the process, and that work never runs")]
    public async Task TerminateInTheEvaluationThatStartsWork_FinishesTheProcess()
    {
        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.TerminateInTheEvaluationThatStartsWork(_host.Log));
        Describe(result);

        // Assert: the process is over, nothing can resume into it, and the work activated in the terminating
        // evaluation was withdrawn rather than started.
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
        Assert.Empty(result.WorkflowState.Bookmarks);
        Assert.Empty(result.WorkflowState.Incidents);
        Assert.DoesNotContain("executed:work", _host.Log.Entries);
    }

    [Fact(DisplayName = "A terminate reached in an evaluation that starts no work finishes the process and retires the live sibling")]
    public async Task TerminateInAnEvaluationThatStartsNoWork_FinishesTheProcessAndRetiresTheLiveSibling()
    {
        // Arrange: both branches are live and the terminate has not been reached yet.
        await _host.RunAsync(BpmnTestProcesses.TerminateInAnEvaluationThatStartsNoWork(_host.Log));

        Assert.Contains("executed:work", _host.Log.Entries);
        Assert.Contains("executed:gate", _host.Log.Entries);
        Assert.NotEqual(true, _host.PersistedScopeMemory().State?.Terminated);

        // Act: 'gate' completes, and its token reaches the terminate end event in an evaluation that starts nothing.
        var result = await _host.FinishWorkAsync("gate");
        Describe(result);

        // Assert
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
        Assert.Empty(result.WorkflowState.Bookmarks);
        Assert.Contains("cancelled:work", _host.Log.Entries);
        Assert.Equal(ActivityStatus.Completed, StatusOf(result, "scope"));
    }

    [Fact(DisplayName = "A behavior fault reached in the evaluation that also starts work faults the workflow, and that work never runs")]
    public async Task FaultInTheEvaluationThatStartsWork_FaultsTheWorkflow()
    {
        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.FaultInTheEvaluationThatStartsWork(_host.Log), typeof(FaultStrategy));
        Describe(result);

        // Assert: the scope itself faulted, as a Fault continuation, rather than waiting on work for a verdict.
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Equal(WorkflowSubStatus.Faulted, result.WorkflowState.SubStatus);

        var incident = Assert.Single(result.WorkflowState.Incidents);
        Assert.Equal("scope", incident.ActivityId);
        Assert.Equal(typeof(BpmnScopeFaultException), incident.Exception?.Type);

        // Assert: the work activated in the faulting evaluation was withdrawn rather than started.
        Assert.Empty(result.WorkflowState.Bookmarks);
        Assert.DoesNotContain("executed:work", _host.Log.Entries);
    }

    /// <summary>Writes what the run ended in to the test output, so a failing assertion comes with the whole picture.</summary>
    private void Describe(RunWorkflowResult result)
    {
        var state = result.WorkflowState;

        // A finished workflow no longer persists its activity contexts, so there may be no scope memory to read.
        var hasScopeMemory = state.ActivityExecutionContexts.Any(x => x.Properties.ContainsKey(BpmnScopeMemory.WorkLedgerPropertyKey));
        var executionState = hasScopeMemory ? _host.PersistedScopeMemory().State : null;

        testOutputHelper.WriteLine($"Workflow: {state.Status}/{state.SubStatus}");
        testOutputHelper.WriteLine($"Bookmarks: [{string.Join(", ", state.Bookmarks.Select(x => x.ActivityId))}]");
        testOutputHelper.WriteLine($"Incidents: [{string.Join(", ", state.Incidents.Select(x => $"{x.ActivityId} ({x.ActivityType}): {x.Message}"))}]");
        testOutputHelper.WriteLine($"Interpreter: Terminated={executionState?.Terminated}, PendingFault={executionState?.PendingFault?.FaultCode}");
        testOutputHelper.WriteLine($"Log: [{string.Join(", ", _host.Log.Entries)}]");
    }

    private static ActivityStatus? StatusOf(RunWorkflowResult result, string activityId) =>
        result.Journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity.Id == activityId)?.Status;
}
