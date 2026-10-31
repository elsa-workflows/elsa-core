using Elsa.Bpmn.Activities;
using Elsa.Workflows;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.Models;
using Xunit.Abstractions;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;

/// <summary>
/// Message and signal boundary events on a task, interrupting and non-interrupting, run end to end through
/// <c>IWorkflowRunner</c>.
/// </summary>
/// <remarks>
/// To the host a message boundary and a signal boundary are the same thing -- a listener armed alongside its host and
/// finished when its trigger arrives -- so each scenario runs for both. What differs between the scenarios is whether
/// the host survives the fire, and whether a listener that never fired is retired when its host completes.
/// </remarks>
public class BpmnCatchBoundaryTests(ITestOutputHelper testOutputHelper)
{
    private readonly BpmnTestHost _host = new(testOutputHelper);

    public static TheoryData<string> Triggers => new() { "message", "signal" };

    [Theory(DisplayName = "An interrupting catch boundary event tears its task down and routes only the boundary path")]
    [MemberData(nameof(Triggers))]
    public async Task InterruptingCatchBoundary_TearsTheTaskDownAndRoutesTheBoundaryPath(string trigger)
    {
        // Arrange: the task and its boundary listener are both running.
        await _host.RunAsync(Process(trigger, interrupting: true), typeof(FaultStrategy));

        Assert.Contains("executed:caught", _host.Log.Entries);

        // Act: the trigger arrives while the task is still running.
        var result = await _host.FinishWorkAsync("caught");

        // Assert: the task was torn down, the boundary path ran, and the task's own outbound path never did.
        Assert.Equal(["executed:task", "executed:caught", "resumed:caught", "cancelled:task", "executed:onCaught"], _host.Log.Entries);
        AssertFinished(result);
    }

    [Theory(DisplayName = "A non-interrupting catch boundary event routes its path while the task keeps running")]
    [MemberData(nameof(Triggers))]
    public async Task NonInterruptingCatchBoundary_RoutesItsPathAndLeavesTheTaskRunning(string trigger)
    {
        // Arrange
        await _host.RunAsync(Process(trigger, interrupting: false), typeof(FaultStrategy));

        // Act: the trigger arrives while the task is still running.
        await _host.FinishWorkAsync("caught");

        // Assert: the boundary path ran, and the task is untouched and still the scope's live work.
        Assert.Contains("executed:onCaught", _host.Log.Entries);
        Assert.DoesNotContain("cancelled:task", _host.Log.Entries);
        Assert.DoesNotContain("executed:afterTask", _host.Log.Entries);
        Assert.Contains(BpmnTestProcesses.BindingRef("task"), _host.LiveWorkOf("scope").Select(work => work.BindingRef));

        // Act: the task finishes on its own.
        var result = await _host.FinishWorkAsync("task");

        // Assert: and its own outbound path runs, which only an uninterrupted task reaches.
        Assert.Contains("executed:afterTask", _host.Log.Entries);
        AssertFinished(result);
    }

    [Theory(DisplayName = "A task completing before its catch boundary fires retires the armed listener")]
    [MemberData(nameof(Triggers))]
    public async Task CatchBoundary_HostCompletingFirst_RetiresTheListener(string trigger)
    {
        // Arrange
        await _host.RunAsync(Process(trigger, interrupting: true), typeof(FaultStrategy));

        // Act: the task finishes before any trigger arrives.
        var result = await _host.FinishWorkAsync("task");

        // Assert: the listener was withdrawn rather than left armed on a task that is already done.
        Assert.Contains("cancelled:caught", _host.Log.Entries);
        Assert.DoesNotContain("executed:onCaught", _host.Log.Entries);
        Assert.Contains("executed:afterTask", _host.Log.Entries);
        AssertFinished(result);
    }

    private BpmnProcess Process(string trigger, bool interrupting) => trigger switch
    {
        "message" => BpmnTestProcesses.MessageBoundary(_host.Log, interrupting),
        "signal" => BpmnTestProcesses.SignalBoundary(_host.Log, interrupting),
        _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger, null)
    };

    private void AssertFinished(RunWorkflowResult result)
    {
        Assert.Empty(result.WorkflowState.Bookmarks);
        Assert.Empty(_host.LiveWorkOf("scope"));
        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }
}
