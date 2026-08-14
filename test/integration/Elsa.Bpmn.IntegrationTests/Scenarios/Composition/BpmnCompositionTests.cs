using Bpmn.Semantics;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;
using Elsa.Bpmn.IntegrationTests.Scenarios.HostPort.Activities;
using Elsa.Common.Models;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.IncidentStrategies;
using Xunit.Abstractions;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.Composition;

/// <summary>
/// A <see cref="BpmnProcess"/> has to work wherever an activity works — nested in another BPMN scope, and (D11)
/// composed into a <c>Flowchart</c> — and it has to stay a nested scope when it is one.
/// </summary>
public class BpmnCompositionTests(ITestOutputHelper testOutputHelper)
{
    private readonly BpmnTestHost _host = new(testOutputHelper);

    [Fact(DisplayName = "A BPMN process composed into a flowchart runs, and the flowchart continues after it")]
    public async Task BpmnProcessInsideAFlowchart_RunsAndTheFlowchartCarriesOn()
    {
        // The runtime machinery is the same in a flowchart as it is under another scope: the process schedules its own
        // work, completes when the interpreter says so, and reports the outcome the flowchart selects a connection on.

        // Arrange
        var before = Work("before");
        var process = BpmnTestProcesses.LinearTask(_host.Log);
        var after = Work("after-process");

        var flowchart = new Flowchart
        {
            Start = before,
            Activities = { before, process, after },
            Connections =
            {
                new Connection(new Endpoint(before, DoneOutcome), new Endpoint(process)),
                // Selected on the BPMN outcome the scope completes with, which is the interpreter's, not Elsa's default.
                new Connection(new Endpoint(process, BpmnInterpreter.DoneOutcomeName), new Endpoint(after))
            }
        };

        // Act
        var result = await _host.RunAsync(flowchart);

        // Assert: the flowchart ran into the process, the process ran its own work, and the flowchart went on.
        Assert.Equal(["executed:before", "executed:only", "executed:after-process"], _host.Log.Entries);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A nested scope runs with the trigger opt-out off, which is its default")]
    public async Task NestedScope_RunsWithTheTriggerOptOutOff()
    {
        // The direction that could be mistaken for success: this is the ordinary case, and it must stay ordinary.
        // Every nested scope in this suite is built without touching the flag, so if the default ever moved, the
        // refusal below would fire here and in every other nested-scope test rather than nowhere.

        // Arrange
        var process = BpmnTestProcesses.NestedSubprocess(_host.Log);

        // Assert: nothing set it, and nothing inferred it.
        Assert.False(NestedScopeOf(process).IsRootScope);

        // Act
        var result = await _host.RunAsync(process);

        // Assert
        Assert.Equal(["executed:subOnly", "executed:after"], _host.Log.Entries);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A nested scope that claims root position is refused, naming it")]
    public async Task NestedScopeClaimingRootPosition_IsRefused()
    {
        // A subprocess body marked as able to start the workflow has already had its start events indexed as ways
        // into the workflow. The host cannot undo that, so it refuses to run the graph rather than papering over it:
        // the alternative is a workflow that runs perfectly while a subprocess quietly answers external messages.

        // Arrange
        var process = BpmnTestProcesses.NestedSubprocess(_host.Log);
        NestedScopeOf(process).IsRootScope = true;

        // Act
        var result = await _host.RunAsync(process, typeof(FaultStrategy));

        // Assert: the scope faulted before starting the nested work...
        Assert.DoesNotContain("executed:subOnly", _host.Log.Entries);
        Assert.Equal(WorkflowSubStatus.Faulted, result.WorkflowState.SubStatus);

        // ...and said which activity, and why.
        var incident = Assert.Single(result.WorkflowState.Incidents);
        Assert.Contains("sub", incident.Exception!.Message);
        Assert.Contains("root scope", incident.Exception.Message);
    }

    [Fact(DisplayName = "A root scope that claims root position runs")]
    public async Task RootScopeClaimingRootPosition_Runs()
    {
        // The refusal is about nesting, not about the flag: the root process is exactly where the flag belongs, and
        // a guard that fired on it would make the flag unusable for what it is for.

        // Arrange
        var process = BpmnTestProcesses.NestedSubprocess(_host.Log);
        process.IsRootScope = true;

        // Act
        var result = await _host.RunAsync(process);

        // Assert
        Assert.Equal(["executed:subOnly", "executed:after"], _host.Log.Entries);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    /// <summary>The one nested BPMN scope of the given process.</summary>
    private static BpmnProcess NestedScopeOf(BpmnProcess process) => process.Activities.OfType<BpmnProcess>().Single();

    private BpmnTestWork Work(string id) => new()
    {
        Id = id,
        Log = _host.Log
    };

    /// <summary>The port an ordinary Elsa activity's completion selects.</summary>
    private const string DoneOutcome = "Done";
}
