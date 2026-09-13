using Elsa.Workflows;
using Elsa.Workflows.IncidentStrategies;

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
public class BpmnCompensationTests : IAsyncDisposable
{
    private readonly BpmnTestHost _host = new(TestContext.Current!.Output.StandardOutput);

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Test]
    [DisplayName("A compensate end event replays every registered handler, in reverse registration order")]
    public async Task CompensatedBookings_ReplaysEveryHandlerInReverseRegistrationOrder()
    {
        // The whole log, not a set of Contains assertions: "all three handlers ran" is also true of a replay that
        // walked the log forwards, and of one that ran them in an order nothing decided. Three registrations are what
        // makes the difference between reversed and merely permuted visible.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.CompensatedBookings(_host.Log), typeof(FaultStrategy));

        // Assert
        await Assert.That(_host.Log.Entries).IsEquivalentTo(
            [
                "executed:bookFlight", "executed:bookHotel", "executed:bookCar",
                "executed:undoCar", "executed:undoHotel", "executed:undoFlight"
            ],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);

        await Assert.That(result.WorkflowState.Incidents).IsEmpty();

        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

    }

    [Test]
    [DisplayName("A compensate throw event naming an activityRef replays only that activity's handler")]
    public async Task TargetedCompensation_ReplaysOnlyTheNamedActivitysHandler()
    {
        // This is also where "a compensation handler is never scheduled from flow" is observable: undoFlight and
        // undoCar are bound as this scope's work exactly like undoHotel is, and the only reason they do not run is
        // that nothing selected them. A container that scheduled handlers from the graph would run all three.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.TargetedCompensation(_host.Log), typeof(FaultStrategy));

        // Assert: only the named activity's handler ran, and the throw then routed its outbound flow.
        await Assert.That(_host.Log.Entries).IsEquivalentTo(
            [
                "executed:bookFlight", "executed:bookHotel", "executed:bookCar",
                "executed:undoHotel", "executed:after"
            ],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);

        await Assert.That(result.WorkflowState.Incidents).IsEmpty();

        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

    }

    [Test]
    [DisplayName("A compensation run torn down mid-replay releases the log entries it claimed and never ran")]
    public async Task CompensationRunCancelledMidReplay_ReleasesTheEntriesItNeverRan()
    {
        // The quiet failure this pins: an entry claimed by a run that was torn down before it ran stays Claimed, which
        // makes it invisible to every later selection. The transaction would then cancel with nothing left to
        // compensate, complete, route its cancel boundary, and finish looking entirely healthy -- with refundCard
        // never having run and no trace that it was skipped.

        // Arrange: chargeCard and reserveSeat registered in that order, so the replay claims both and runs them in
        // reverse. releaseSeat is the head and blocks; refundCard is claimed and has not started.
        await _host.RunAsync(BpmnTestProcesses.CompensationRunCancelledMidReplay(_host.Log), typeof(FaultStrategy));

        await Assert.That(_host.Log.Occurrences("executed:releaseSeat")).IsEqualTo(1);

        await Assert.That(_host.Log.Entries).DoesNotContain("executed:refundCard");


        // Act: the other branch cancels the transaction, which stops the replay's coordinating token.
        await _host.FinishWorkAsync("fraudCheck");

        // Assert: the released entries are registered again, so the cancellation's own replay claims them, and the head
        // handler starts a second time -- the first half of the release being real.
        //
        // The second start is a *replacement*, not a duplicate, and that is what valence-works/bpmn#13 fixed in
        // Bpmn.Semantics 0.2.0. CancelTransaction used to abandon the live work it was superseding without issuing a
        // CancelWorkSubtree for it, so the host's original ledger record and bookmark for the releaseSeat slot
        // survived alongside the replay's freshly started one -- two live records for one (BindingRef, IterationId)
        // slot, which BpmnHostSnapshot documents as a host obligation never to allow, and which the interpreter's own
        // BuildLiveWorkHandles then collapses last-wins. Upstream took the wide fix: every token a cancelled
        // transaction abandons now gets a real teardown, not just the compensation handler being re-started.
        //
        // So all three of these are the fix, and each fails differently if it regresses: the handler runs twice
        // (release), the first run is torn down (teardown), and the slot is left holding exactly one record
        // (the invariant).
        await Assert.That(_host.Log.Occurrences("executed:releaseSeat")).IsEqualTo(2);

        await Assert.That(_host.Log.Entries).Contains("cancelled:releaseSeat");


        var releaseSeatBindingRef = BpmnTestProcesses.BindingRef("releaseSeat");
        var releaseSeatLiveRecords = _host.LiveWorkOf("sub").Count(record => record.BindingRef == releaseSeatBindingRef);
        await Assert.That(releaseSeatLiveRecords).IsEqualTo(1);


        // And the invariant holds at the moment that matters, not just once the dust settles. 0.2.0 states the
        // at-most-one-live-unit-per-slot rule as holding after a command batch is applied in full and in order,
        // because an interrupting path may emit the replacement StartWork ahead of the CancelWorkSubtree it
        // supersedes. This snapshot is taken inside the replacement's own execution -- overwritten on each start, so
        // it is the second one -- and a host that applied the batch out of order, or keyed its ledger by slot rather
        // than by handle, would be holding two records here even though the count above settles at one.
        await Assert.That(_host.Log.Snapshot("liveWork@releaseSeat")).IsEquivalentTo([releaseSeatBindingRef], TUnit.Assertions.Enums.CollectionOrdering.Matching);


        // And the entry that was claimed but never ran is reached once that head handler finishes: the other half.
        var result = await _host.FinishWorkAsync("releaseSeat");

        await Assert.That(_host.Log.Occurrences("executed:refundCard")).IsEqualTo(1);


        // The transaction still completes Cancelled, so the enclosing scope takes the boundary path and not the
        // ordinary sequence flow.
        await Assert.That(_host.Log.Entries).Contains("executed:unwind");

        await Assert.That(_host.Log.Entries).DoesNotContain("executed:after");


        await Assert.That(result.WorkflowState.Incidents).IsEmpty();

        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

    }

    [Test]
    [DisplayName("A transaction completing Cancelled with no cancel boundary attached faults, rather than completing quietly")]
    public async Task CancelledTransactionWithoutCancelBoundary_Faults()
    {
        // The conservative direction, and the interpreter takes it: graph validation cannot see into the nested
        // definition to know a cancel end event is in there, so an unroutable cancellation is only discoverable while
        // running. Treating it as an ordinary completion would send the token down the sequence flow that leads to
        // 'after' -- a transaction that cancelled itself, followed by the work it cancelled itself to avoid.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.CancelledTransactionWithoutCancelBoundary(_host.Log), typeof(FaultStrategy));

        // Assert
        await Assert.That(_host.Log.Entries).DoesNotContain("executed:after");

        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Faulted);


        var incident = (await Assert.That(result.WorkflowState.Incidents).HasSingleItem())!;


        await Assert.That(incident.Exception!.Message).Contains("bpmn.transaction.cancelled-unhandled", StringComparison.CurrentCulture);

    }

    [Test]
    [DisplayName("A subprocess replays its own compensation log, and the enclosing scope replays the subprocess itself")]
    public async Task CompensationInsideSubprocess_ReplaysEachScopesOwnLog()
    {
        // Two logs, one per scope. The body's two handlers run in its own reverse order before it completes, and the
        // enclosing scope's single registration -- the subprocess's own successful completion, made compensable by the
        // boundary attached to it -- is what its compensate end event replays. A scope reaching into another's log
        // would show up here as a handler running in the wrong scope's replay, or twice.

        // Act
        var result = await _host.RunAsync(BpmnTestProcesses.CompensationInsideSubprocess(_host.Log), typeof(FaultStrategy));

        // Assert
        await Assert.That(_host.Log.Entries).IsEquivalentTo(
            [
                "executed:subCharge", "executed:subShip",
                "executed:subRecall", "executed:subRefund",
                "executed:undoSub"
            ],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);

        await Assert.That(result.WorkflowState.Incidents).IsEmpty();

        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

    }
}
