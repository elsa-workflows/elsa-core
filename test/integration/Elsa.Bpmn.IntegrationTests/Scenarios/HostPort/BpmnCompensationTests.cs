using Elsa.Workflows;
using Elsa.Workflows.IncidentStrategies;
using Xunit.Abstractions;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;

/// <summary>
/// Compensation, transaction cancellation, and the two things about them that are the host's to get right: a
/// compensation handler binds work but is reached only by replay, and a transaction that completes <c>Cancelled</c>
/// carries that outcome to its enclosing scope.
/// </summary>
/// <remarks>
/// <para>
/// The compensation log, the reverse ordering, and the claim/release all belong to the interpreter. What arrives here
/// is an ordinary <c>StartWork</c>, applied like any other, so these are process-level tests rather than tests of a
/// compensation-specific code path — there is none.
/// </para>
/// <para>
/// Every process runs under <see cref="FaultStrategy"/>. Compensation's failure mode is a quiet one: a replay that
/// claims nothing, a handler that is skipped, an unroutable cancellation treated as an ordinary completion all leave
/// a workflow that finished and reported nothing. Faulting rather than absorbing into an incident is what keeps a
/// teardown this host cannot honour from being buried under work that carried on regardless.
/// </para>
/// </remarks>
public class BpmnCompensationTests(ITestOutputHelper testOutputHelper)
{
    private readonly BpmnTestHost _host = new(testOutputHelper);

    [Fact(DisplayName = "A compensate end event replays every registered handler, in reverse registration order")]
    public async Task CompensatedBookings_ReplaysEveryHandlerInReverseRegistrationOrder()
    {
        // The whole log, not a set of Contains assertions: "all three handlers ran" is also true of a replay that
        // walked the log forwards, and of one that ran them in an order nothing decided. Three registrations are what
        // makes the difference between reversed and merely permuted visible.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.CompensatedBookings(_host.Log), typeof(FaultStrategy));

        // Assert
        Assert.Equal(
            [
                "executed:bookFlight", "executed:bookHotel", "executed:bookCar",
                "executed:undoCar", "executed:undoHotel", "executed:undoFlight"
            ],
            _host.Log.Entries);

        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A compensate throw event naming an activityRef replays only that activity's handler")]
    public async Task TargetedCompensation_ReplaysOnlyTheNamedActivitysHandler()
    {
        // This is also where "a compensation handler is never scheduled from flow" is observable: undoFlight and
        // undoCar are bound as this scope's work exactly like undoHotel is, and the only reason they do not run is
        // that nothing selected them. A container that scheduled handlers from the graph would run all three.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.TargetedCompensation(_host.Log), typeof(FaultStrategy));

        // Assert: only the named activity's handler ran, and the throw then routed its outbound flow.
        Assert.Equal(
            [
                "executed:bookFlight", "executed:bookHotel", "executed:bookCar",
                "executed:undoHotel", "executed:after"
            ],
            _host.Log.Entries);

        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A compensation run torn down mid-replay releases the log entries it claimed and never ran")]
    public async Task CompensationRunCancelledMidReplay_ReleasesTheEntriesItNeverRan()
    {
        // The quiet failure this pins: an entry claimed by a run that was torn down before it ran stays Claimed, which
        // makes it invisible to every later selection. The transaction would then cancel with nothing left to
        // compensate, complete, route its cancel boundary, and finish looking entirely healthy -- with refundCard
        // never having run and no trace that it was skipped.

        // Arrange: chargeCard and reserveSeat registered in that order, so the replay claims both and runs them in
        // reverse. releaseSeat is the head and blocks; refundCard is claimed and has not started.
        await _host.RunAsync(BpmnTestProcesses.CompensationRunCancelledMidReplay(_host.Log), typeof(FaultStrategy));

        Assert.Equal(1, _host.Log.Occurrences("executed:releaseSeat"));
        Assert.DoesNotContain("executed:refundCard", _host.Log.Entries);

        // Act: the other branch cancels the transaction, which stops the replay's coordinating token.
        await _host.FinishWorkAsync("fraudCheck");

        // Assert: the released entries are registered again, so the cancellation's own replay claims them -- the head
        // handler starting a second time is both the first half of the release being real *and* the symptom of
        // valence-works/bpmn#13: BpmnInterpreter.CancelTransaction drops the live work it is abandoning without ever
        // producing a PendingTeardown/CancelWorkSubtree command for it, so the host's original ledger record and
        // bookmark for the releaseSeat slot survive untouched alongside the replay's freshly started one. The scope
        // now holds two live records for the same (BindingRef, IterationId) slot -- pinned below so this fails the
        // moment the library fix lands, at which point both counts become 1. Filed from here as #7959, closed as
        // routed upstream; the applier is deliberately unpatched so this stays visible rather than worked around.
        //
        // The flip may not be mechanical. Upstream is choosing between tearing down only the compensation handler it
        // re-starts and tearing down every abandoned token; the second also cancels transaction branches still in
        // flight, which can move other scenarios in this suite. Check which shipped when the Bpmn.Semantics pin moves.
        Assert.Equal(2, _host.Log.Occurrences("executed:releaseSeat"));

        var releaseSeatBindingRef = BpmnTestProcesses.BindingRef("releaseSeat");
        var releaseSeatLiveRecords = _host.LiveWorkOf("sub").Count(record => record.BindingRef == releaseSeatBindingRef);
        Assert.Equal(2, releaseSeatLiveRecords);

        // And the entry that was claimed but never ran is reached once that head handler finishes: the other half.
        var result = await _host.FinishWorkAsync("releaseSeat");

        Assert.Equal(1, _host.Log.Occurrences("executed:refundCard"));

        // The transaction still completes Cancelled, so the enclosing scope takes the boundary path and not the
        // ordinary sequence flow.
        Assert.Contains("executed:unwind", _host.Log.Entries);
        Assert.DoesNotContain("executed:after", _host.Log.Entries);

        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A transaction completing Cancelled with no cancel boundary attached faults, rather than completing quietly")]
    public async Task CancelledTransactionWithoutCancelBoundary_Faults()
    {
        // The conservative direction, and the interpreter takes it: graph validation cannot see into the nested
        // definition to know a cancel end event is in there, so an unroutable cancellation is only discoverable while
        // running. Treating it as an ordinary completion would send the token down the sequence flow that leads to
        // 'after' -- a transaction that cancelled itself, followed by the work it cancelled itself to avoid.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.CancelledTransactionWithoutCancelBoundary(_host.Log), typeof(FaultStrategy));

        // Assert
        Assert.DoesNotContain("executed:after", _host.Log.Entries);
        Assert.Equal(WorkflowSubStatus.Faulted, result.WorkflowState.SubStatus);

        var incident = Assert.Single(result.WorkflowState.Incidents);

        Assert.Contains("bpmn.transaction.cancelled-unhandled", incident.Exception!.Message);
    }

    [Fact(DisplayName = "A subprocess replays its own compensation log, and the enclosing scope replays the subprocess itself")]
    public async Task CompensationInsideSubprocess_ReplaysEachScopesOwnLog()
    {
        // Two logs, one per scope. The body's two handlers run in its own reverse order before it completes, and the
        // enclosing scope's single registration -- the subprocess's own successful completion, made compensable by the
        // boundary attached to it -- is what its compensate end event replays. A scope reaching into another's log
        // would show up here as a handler running in the wrong scope's replay, or twice.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.CompensationInsideSubprocess(_host.Log), typeof(FaultStrategy));

        // Assert
        Assert.Equal(
            [
                "executed:subCharge", "executed:subShip",
                "executed:subRecall", "executed:subRefund",
                "executed:undoSub"
            ],
            _host.Log.Entries);

        Assert.Empty(result.WorkflowState.Incidents);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }
}
