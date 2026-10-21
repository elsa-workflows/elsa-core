using Elsa.Bpmn.Hosting;
using Elsa.Workflows;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.Models;
using Xunit.Abstractions;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;

/// <summary>
/// The inclusive and event-based gateways, run end to end through <c>IWorkflowRunner</c>.
/// </summary>
/// <remarks>
/// Both are routing the interpreter owns, so what is the host's to get right is narrower than it looks: start the
/// work the selected flows reach and nothing else, and withdraw exactly the work the interpreter tells it to. Every
/// process runs under <see cref="FaultStrategy"/>, so a gateway that faults is a failed test rather than an incident
/// the workflow carried on past.
/// </remarks>
public class BpmnGatewayTests(ITestOutputHelper testOutputHelper)
{
    private readonly BpmnTestHost _host = new(testOutputHelper);

    [Fact(DisplayName = "An inclusive split takes every flow its decision selected, and the inclusive join waits only for those")]
    public async Task InclusiveGateway_SplitsOnTheSelectedFlowsAndJoinsOnlyThose()
    {
        // Arrange: the decision selects 'left' and 'right', and both block.
        await _host.RunAsync(BpmnTestProcesses.InclusiveSplitAndJoin(_host.Log), typeof(FaultStrategy));

        // Assert: both selected branches started, and the unselected one did not.
        Assert.Contains("executed:left", _host.Log.Entries);
        Assert.Contains("executed:right", _host.Log.Entries);
        Assert.DoesNotContain("executed:middle", _host.Log.Entries);

        // Act: one activated branch finishes.
        await _host.FinishWorkAsync("left");

        // Assert: the join is still waiting for the other activated branch.
        Assert.DoesNotContain("executed:after", _host.Log.Entries);

        // Act: the other activated branch finishes.
        var result = await _host.FinishWorkAsync("right");

        // Assert: the join fired once, without ever waiting for the branch that was never activated.
        Assert.Equal(1, _host.Log.Occurrences("executed:after"));
        Assert.DoesNotContain("executed:middle", _host.Log.Entries);
        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);

        // The join's own decision is journaled against the join element, which binds no work of its own.
        Assert.Single(Diagnostics(result), payload => payload.ElementId == "join" && payload.Kind == BpmnDiagnosticEventNames.Joined);
    }

    [Fact(DisplayName = "An event-based gateway race won by the message withdraws the timer branch")]
    public async Task EventBasedGateway_MessageWins_WithdrawsTheTimer()
    {
        // Arrange: both catches are armed.
        await _host.RunAsync(BpmnTestProcesses.EventBasedGatewayRace(_host.Log), typeof(FaultStrategy));

        Assert.Contains("executed:payment", _host.Log.Entries);
        Assert.Contains("executed:deadline", _host.Log.Entries);

        // Act: the message arrives first.
        var result = await _host.FinishWorkAsync("payment");

        // Assert: the message path ran, the timer was withdrawn, and the timer path never ran.
        Assert.Contains("executed:ship", _host.Log.Entries);
        Assert.Contains("cancelled:deadline", _host.Log.Entries);
        Assert.DoesNotContain("executed:cancelOrder", _host.Log.Entries);
        AssertFinishedWithNothingLeftArmed(result);
    }

    [Fact(DisplayName = "An event-based gateway race won by the timer withdraws the message branch")]
    public async Task EventBasedGateway_TimerWins_WithdrawsTheMessage()
    {
        // Arrange
        await _host.RunAsync(BpmnTestProcesses.EventBasedGatewayRace(_host.Log), typeof(FaultStrategy));

        // Act: the timer fires first.
        var result = await _host.FinishWorkAsync("deadline");

        // Assert
        Assert.Contains("executed:cancelOrder", _host.Log.Entries);
        Assert.Contains("cancelled:payment", _host.Log.Entries);
        Assert.DoesNotContain("executed:ship", _host.Log.Entries);
        AssertFinishedWithNothingLeftArmed(result);
    }

    private void AssertFinishedWithNothingLeftArmed(RunWorkflowResult result)
    {
        // A withdrawn catch that left a bookmark behind is a listener a late message could still resume into.
        Assert.Empty(result.WorkflowState.Bookmarks);
        Assert.Empty(_host.LiveWorkOf("scope"));
        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    private static IEnumerable<BpmnDiagnosticLogPayload> Diagnostics(RunWorkflowResult result) =>
        result.Journal.WorkflowExecutionLogEntries
            .Where(entry => entry.Source == BpmnDiagnosticEventNames.Source)
            .Select(entry => Assert.IsType<BpmnDiagnosticLogPayload>(entry.Payload));
}
