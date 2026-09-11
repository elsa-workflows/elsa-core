using Elsa.Bpmn.Hosting;
using Elsa.Workflows.Models;
using Xunit.Abstractions;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;

/// <summary>
/// D1: the interpreter's BPMN diagnostics are projected onto the scope's own execution log, keyed by element id, so
/// Studio's instance viewer has something to read for a gateway, an intermediate event or a sequence flow — none of
/// which has an activity id of its own under Option A.
/// </summary>
public class BpmnDiagnosticsProjectionTests(ITestOutputHelper testOutputHelper)
{
    private readonly BpmnTestHost _host = new(testOutputHelper);

    [Fact(DisplayName = "Diagnostics for a parallel split, both flows and the join are projected onto the scope's own journal, none duplicated after a suspend and a resume")]
    public async Task ParallelSplitAndJoin_ProjectsDiagnosticsForTheGatewayAndFlows_WithNoDuplicatesAfterSuspendAndResume()
    {
        // Arrange & Act: a parallel split into two branches that both block, so the scope suspends mid-way and is
        // resumed twice -- once per branch -- round-tripping the workflow state through Elsa's own serializer
        // between each step, exactly as a real suspend and resume would.
        var entries = new List<WorkflowExecutionLogEntry>();

        entries.AddRange((await _host.RunAsync(BpmnTestProcesses.ParallelSplitAndJoinBlocking(_host.Log))).Journal.WorkflowExecutionLogEntries);
        _host.RoundTripStateThroughJson();
        entries.AddRange((await _host.FinishWorkAsync("left")).Journal.WorkflowExecutionLogEntries);
        _host.RoundTripStateThroughJson();
        entries.AddRange((await _host.FinishWorkAsync("right")).Journal.WorkflowExecutionLogEntries);

        var payloads = entries
            .Where(x => x.Source == BpmnDiagnosticEventNames.Source)
            .Select(x => Assert.IsType<BpmnDiagnosticLogPayload>(x.Payload))
            .ToList();

        // Assert: the split gateway, both outbound flows and the join each left a trace, keyed by element id (or,
        // for a flow, by flow id).
        Assert.Contains(payloads, p => p.ElementId == "split");
        Assert.Contains(payloads, p => p.FlowId == "flow-split-left");
        Assert.Contains(payloads, p => p.FlowId == "flow-split-right");
        Assert.Contains(payloads, p => p.ElementId == "join" && p.Kind == BpmnDiagnosticEventNames.Joined);

        // ...and nothing the suspend-and-resume round trip crossed was projected twice: each diagnostic id the
        // interpreter ever minted for this scope appears in the merged journal at most once.
        var diagnosticIds = payloads.Select(p => p.DiagnosticId).ToList();
        Assert.Equal(diagnosticIds.Distinct().Count(), diagnosticIds.Count);
    }

    [Fact(DisplayName = "The scope's own start and completion are never projected as diagnostics")]
    public async Task Scope_DoesNotProjectItsOwnStartOrCompletion()
    {
        var result = await _host.RunAsync(BpmnTestProcesses.LinearTask(_host.Log));

        var diagnostics = result.Journal.WorkflowExecutionLogEntries.Where(x => x.Source == BpmnDiagnosticEventNames.Source).ToList();

        // The scope's own completion carries the "Completed" kind and names no element...
        Assert.DoesNotContain(diagnostics, x => x.EventName == BpmnDiagnosticEventNames.Completed);

        // ...and its own start is the initial token emitted at the "start" element -- present in the underlying
        // interpreter state, but not projected, unlike every other element a token actually flows through.
        Assert.DoesNotContain(diagnostics, x => ((BpmnDiagnosticLogPayload)x.Payload!).ElementId == "start");

        // Sanity: the linear task itself did leave a trace, so the assertions above are not vacuous.
        Assert.Contains(diagnostics, x => ((BpmnDiagnosticLogPayload)x.Payload!).ElementId == "only");
    }

    [Fact(DisplayName = "Diagnostics are written on the scope's own activity execution context, never a child's")]
    public async Task Diagnostics_AreWrittenOnTheScopesOwnContext()
    {
        var result = await _host.RunAsync(BpmnTestProcesses.LinearTask(_host.Log));

        var diagnostics = result.Journal.WorkflowExecutionLogEntries.Where(x => x.Source == BpmnDiagnosticEventNames.Source).ToList();

        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, x => Assert.Equal("scope", x.ActivityId));
    }
}
