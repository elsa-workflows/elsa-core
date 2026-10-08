using Elsa.Bpmn.Hosting;
using Elsa.Workflows.IncidentStrategies;
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

    [Fact(DisplayName = "Only the scope's own completion, which names neither an element nor a flow, is never projected as a diagnostic")]
    public async Task Scope_DoesNotProjectItsOwnCompletion()
    {
        var result = await _host.RunAsync(BpmnTestProcesses.LinearTask(_host.Log));

        var diagnostics = result.Journal.WorkflowExecutionLogEntries.Where(x => x.Source == BpmnDiagnosticEventNames.Source).ToList();

        // The scope's own completion carries the "Completed" kind and names neither an element nor a flow...
        Assert.DoesNotContain(diagnostics, x => x.EventName == BpmnDiagnosticEventNames.Completed);

        // ...but its own start -- the initial token emitted at the "start" element -- names that element, and unlike
        // the scope's completion is projected: it is what Studio's overlay lights up for the start event.
        Assert.Contains(diagnostics, x => ((BpmnDiagnosticLogPayload)x.Payload!).ElementId == "start" && x.EventName == BpmnDiagnosticEventNames.TokenEmitted);

        // Sanity: the linear task itself did leave a trace, so the assertions above are not vacuous.
        Assert.Contains(diagnostics, x => ((BpmnDiagnosticLogPayload)x.Payload!).ElementId == "only");
    }

    [Fact(DisplayName = "An error boundary's token emission is projected even though it carries no inbound flow")]
    public async Task ErrorBoundaryCaught_ProjectsTheBoundarysTokenEmission()
    {
        var result = await _host.RunAsync(BpmnTestProcesses.ErrorBoundaryCaught(_host.Log), typeof(FaultStrategy));

        var diagnostics = result.Journal.WorkflowExecutionLogEntries.Where(x => x.Source == BpmnDiagnosticEventNames.Source).ToList();

        // The boundary fires a token of its own -- no sequence flow feeds it -- so FlowId is null, but it names the
        // boundary element, and the previous (over-broad) rule dropped it on that account alone.
        Assert.Contains(diagnostics, x =>
            x.EventName == BpmnDiagnosticEventNames.TokenEmitted &&
            ((BpmnDiagnosticLogPayload)x.Payload!).ElementId == "oops" &&
            string.IsNullOrEmpty(((BpmnDiagnosticLogPayload)x.Payload!).FlowId));
    }

    [Fact(DisplayName = "A scope persisted before the diagnostics cursor existed does not replay its historical diagnostics on resume")]
    public async Task MissingDiagnosticsCursor_DoesNotReplayDiagnosticsFromBeforeTheSuspend_ButStillProjectsNewOnes()
    {
        // Arrange & Act: run to a suspend with both branches blocked, so the scope's execution state already
        // carries diagnostics for the split and both outbound flows...
        var beforeResume = await _host.RunAsync(BpmnTestProcesses.ParallelSplitAndJoinBlocking(_host.Log));
        var historicalDiagnosticIds = beforeResume.Journal.WorkflowExecutionLogEntries
            .Where(x => x.Source == BpmnDiagnosticEventNames.Source)
            .Select(x => ((BpmnDiagnosticLogPayload)x.Payload!).DiagnosticId)
            .ToList();
        Assert.NotEmpty(historicalDiagnosticIds);

        // ...then delete the cursor property, simulating a scope that was suspended before this feature existed:
        // diagnostics in its state, but nothing recording how many of them are already journaled.
        _host.RemoveDiagnosticsCursor();

        var afterResume = await _host.FinishWorkAsync("left");
        var resumedDiagnostics = afterResume.Journal.WorkflowExecutionLogEntries
            .Where(x => x.Source == BpmnDiagnosticEventNames.Source)
            .Select(x => (BpmnDiagnosticLogPayload)x.Payload!)
            .ToList();

        // Assert: none of the diagnostics that were already in the state before this evaluation is projected again...
        Assert.DoesNotContain(resumedDiagnostics, p => historicalDiagnosticIds.Contains(p.DiagnosticId));

        // ...but the diagnostic this evaluation actually produced -- the join now waiting on its left inbound flow
        // -- is projected, so the missing cursor did not also make the scope swallow genuinely new diagnostics.
        Assert.Contains(resumedDiagnostics, p => p.ElementId == "join" && p.FlowId == "flow-left-join");
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
