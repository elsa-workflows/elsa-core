using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.IntegrationTests.Scenarios.HostPort.Activities;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;

internal static partial class BpmnTestProcesses
{
    /// <summary>
    /// Three bookings, each carrying a compensation boundary event, and a compensate end event that replays the lot.
    /// </summary>
    /// <remarks>
    /// Three rather than two: with two, a handler order that merely happened to be reversed is indistinguishable from
    /// one that swapped a pair, and a replay that walked the log forwards would still run every handler. The three
    /// handlers bind work like anything else, and nothing in the graph flows into them — the only thing that can run
    /// them is the replay.
    /// </remarks>
    public static BpmnProcess CompensatedBookings(BpmnTestLog log)
    {
        var definition = new BpmnProcessBuilder("compensated-bookings")
            .StartEvent("start")
            .Task("bookFlight", bindingRef: BindingRef("bookFlight"))
            .Task("bookHotel", bindingRef: BindingRef("bookHotel"))
            .Task("bookCar", bindingRef: BindingRef("bookCar"))
            .EndEvent("undoEverything", null, Compensation())
            .Element(CompensationBoundary("flightCompensated", attachedTo: "bookFlight", handler: "undoFlight"))
            .Element(CompensationBoundary("hotelCompensated", attachedTo: "bookHotel", handler: "undoHotel"))
            .Element(CompensationBoundary("carCompensated", attachedTo: "bookCar", handler: "undoCar"))
            .Element(CompensationHandler("undoFlight"))
            .Element(CompensationHandler("undoHotel"))
            .Element(CompensationHandler("undoCar"))
            .ConnectSequence("start", "bookFlight", "bookHotel", "bookCar", "undoEverything")
            .Build();

        return Scope(
            "scope",
            definition,
            Immediate("bookFlight", log),
            Immediate("bookHotel", log),
            Immediate("bookCar", log),
            Immediate("undoFlight", log),
            Immediate("undoHotel", log),
            Immediate("undoCar", log));
    }

    /// <summary>
    /// The same three bookings, compensated by an intermediate throw event naming one of them in its
    /// <c>activityRef</c>, and a task after the throw that its outbound flow reaches once the replay is done.
    /// </summary>
    public static BpmnProcess TargetedCompensation(BpmnTestLog log)
    {
        var definition = new BpmnProcessBuilder("targeted-compensation")
            .StartEvent("start")
            .Task("bookFlight", bindingRef: BindingRef("bookFlight"))
            .Task("bookHotel", bindingRef: BindingRef("bookHotel"))
            .Task("bookCar", bindingRef: BindingRef("bookCar"))
            .IntermediateThrowEvent("undoHotelOnly", Compensation(activityRef: "bookHotel"))
            .Task("after", bindingRef: BindingRef("after"))
            .EndEvent("end")
            .Element(CompensationBoundary("flightCompensated", attachedTo: "bookFlight", handler: "undoFlight"))
            .Element(CompensationBoundary("hotelCompensated", attachedTo: "bookHotel", handler: "undoHotel"))
            .Element(CompensationBoundary("carCompensated", attachedTo: "bookCar", handler: "undoCar"))
            .Element(CompensationHandler("undoFlight"))
            .Element(CompensationHandler("undoHotel"))
            .Element(CompensationHandler("undoCar"))
            .ConnectSequence("start", "bookFlight", "bookHotel", "bookCar", "undoHotelOnly", "after", "end")
            .Build();

        return Scope(
            "scope",
            definition,
            Immediate("bookFlight", log),
            Immediate("bookHotel", log),
            Immediate("bookCar", log),
            Immediate("undoFlight", log),
            Immediate("undoHotel", log),
            Immediate("undoCar", log),
            Immediate("after", log));
    }

    /// <summary>
    /// A compensation log inside an embedded subprocess, and a second one in the enclosing scope that compensates the
    /// subprocess itself.
    /// </summary>
    /// <remarks>
    /// Two logs, one per scope, and neither can see the other: the body replays its own two handlers before it
    /// completes, and the enclosing scope registers exactly one entry — the subprocess's own successful completion,
    /// which its attached compensation boundary makes compensable — and replays that.
    /// </remarks>
    public static BpmnProcess CompensationInsideSubprocess(BpmnTestLog log)
    {
        var body = new BpmnProcessBuilder("compensating-subprocess-body")
            .StartEvent("subStart")
            .Task("subCharge", bindingRef: BindingRef("subCharge"))
            .Task("subShip", bindingRef: BindingRef("subShip"))
            .EndEvent("subUndo", null, Compensation())
            .Element(CompensationBoundary("subChargeCompensated", attachedTo: "subCharge", handler: "subRefund"))
            .Element(CompensationBoundary("subShipCompensated", attachedTo: "subShip", handler: "subRecall"))
            .Element(CompensationHandler("subRefund"))
            .Element(CompensationHandler("subRecall"))
            .ConnectSequence("subStart", "subCharge", "subShip", "subUndo")
            .Build();

        var definition = new BpmnProcessBuilder("compensation-inside-subprocess")
            .StartEvent("start")
            .SubProcess("sub", bindingRef: BindingRef("sub"))
            .EndEvent("undoOuter", null, Compensation())
            .Element(CompensationBoundary("subCompensated", attachedTo: "sub", handler: "undoSub"))
            .Element(CompensationHandler("undoSub"))
            .ConnectSequence("start", "sub", "undoOuter")
            .Build();

        var nested = Scope(
            "sub",
            body,
            Immediate("subCharge", log),
            Immediate("subShip", log),
            Immediate("subRefund", log),
            Immediate("subRecall", log));

        return Scope("scope", definition, nested, Immediate("undoSub", log));
    }

    /// <summary>
    /// A transaction subprocess that cancels itself from the inside, and a cancel boundary event on the transaction
    /// that routes the cancellation.
    /// </summary>
    /// <remarks>
    /// The nested scope completes with the <c>Cancelled</c> outcome rather than <c>Done</c>, and the enclosing scope
    /// only reaches the boundary path if that outcome survives the trip through the parent's completion callback.
    /// Nothing else in the process distinguishes the two: with the outcome dropped the parent simply carries on down
    /// the ordinary sequence flow, which is a completion that looks entirely successful.
    /// </remarks>
    public static BpmnProcess CancelledTransactionSubprocess(BpmnTestLog log)
    {
        var body = new BpmnProcessBuilder("transaction-body")
            .Transaction()
            .StartEvent("subStart")
            .Task("subWork", bindingRef: BindingRef("subWork"))
            .EndEvent("subCancelled", null, Cancel())
            .ConnectSequence("subStart", "subWork", "subCancelled")
            .Build();

        var definition = new BpmnProcessBuilder("cancelled-transaction-subprocess")
            .StartEvent("start")
            .SubProcess("sub", bindingRef: BindingRef("sub"), isTransaction: true)
            .Task("after", bindingRef: BindingRef("after"))
            .EndEvent("end")
            .BoundaryEvent("cancelled", attachedTo: "sub", eventDefinition: Cancel())
            .Task("unwind", bindingRef: BindingRef("unwind"))
            .EndEvent("unwound")
            .ConnectSequence("start", "sub", "after", "end")
            .ConnectSequence("cancelled", "unwind", "unwound")
            .Build();

        var nested = Scope("sub", body, Immediate("subWork", log));

        return Scope("scope", definition, nested, Immediate("after", log), Immediate("unwind", log));
    }

    /// <summary>
    /// A transaction subprocess that cancels itself from the inside, with nothing on the enclosing scope to route the
    /// cancellation.
    /// </summary>
    /// <remarks>
    /// The same shape as <see cref="CancelledTransactionSubprocess"/> minus the cancel boundary event. Graph
    /// validation cannot see into the nested definition to know a cancel end event is in there, so an unroutable
    /// cancellation is an execution-time rule: the enclosing scope faults rather than treating the transaction as an
    /// ordinary completion and carrying on down the sequence flow.
    /// </remarks>
    public static BpmnProcess CancelledTransactionWithoutCancelBoundary(BpmnTestLog log)
    {
        var body = new BpmnProcessBuilder("unroutable-cancel-body")
            .Transaction()
            .StartEvent("subStart")
            .Task("subWork", bindingRef: BindingRef("subWork"))
            .EndEvent("subCancelled", null, Cancel())
            .ConnectSequence("subStart", "subWork", "subCancelled")
            .Build();

        var definition = new BpmnProcessBuilder("unroutable-cancel")
            .StartEvent("start")
            .SubProcess("sub", bindingRef: BindingRef("sub"), isTransaction: true)
            .Task("after", bindingRef: BindingRef("after"))
            .EndEvent("end")
            .ConnectSequence("start", "sub", "after", "end")
            .Build();

        var nested = Scope("sub", body, Immediate("subWork", log));

        return Scope("scope", definition, nested, Immediate("after", log));
    }

    /// <summary>
    /// A transaction subprocess that starts a compensation replay on one branch and cancels itself on the other while
    /// that replay is still running, so the replay's claimed-but-unrun log entries are torn down mid-run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The shape is what makes the release observable. <c>chargeCard</c> and <c>reserveSeat</c> both complete and
    /// register, in that order, so the replay the intermediate throw opens claims both and runs them in reverse:
    /// <c>releaseSeat</c> first, which blocks, leaving <c>refundCard</c> claimed and never started. Cancelling the
    /// transaction from the other branch stops the replay's coordinating token, which is the only thing in scope that
    /// tears a run down mid-flight.
    /// </para>
    /// <para>
    /// <c>fraudCheck</c> blocks so a test decides when the cancellation happens, rather than racing the replay.
    /// </para>
    /// </remarks>
    public static BpmnProcess CompensationRunCancelledMidReplay(BpmnTestLog log)
    {
        var body = new BpmnProcessBuilder("mid-replay-cancel-body")
            .Transaction()
            .StartEvent("subStart")
            .ParallelGateway("subSplit")
            .Task("chargeCard", bindingRef: BindingRef("chargeCard"))
            .Task("reserveSeat", bindingRef: BindingRef("reserveSeat"))
            .IntermediateThrowEvent("rollBack", Compensation())
            .EndEvent("rolledBack")
            .Task("fraudCheck", bindingRef: BindingRef("fraudCheck"))
            .EndEvent("subCancelled", null, Cancel())
            .Element(CompensationBoundary("cardCompensated", attachedTo: "chargeCard", handler: "refundCard"))
            .Element(CompensationBoundary("seatCompensated", attachedTo: "reserveSeat", handler: "releaseSeat"))
            .Element(CompensationHandler("refundCard"))
            .Element(CompensationHandler("releaseSeat"))
            .ConnectSequence("subStart", "subSplit")
            .Connect("subSplit", "chargeCard")
            .ConnectSequence("chargeCard", "reserveSeat", "rollBack", "rolledBack")
            .Connect("subSplit", "fraudCheck")
            .ConnectSequence("fraudCheck", "subCancelled")
            .Build();

        var definition = new BpmnProcessBuilder("mid-replay-cancel")
            .StartEvent("start")
            .SubProcess("sub", bindingRef: BindingRef("sub"), isTransaction: true)
            .Task("after", bindingRef: BindingRef("after"))
            .EndEvent("end")
            .BoundaryEvent("cancelled", attachedTo: "sub", eventDefinition: Cancel())
            .Task("unwind", bindingRef: BindingRef("unwind"))
            .EndEvent("unwound")
            .ConnectSequence("start", "sub", "after", "end")
            .ConnectSequence("cancelled", "unwind", "unwound")
            .Build();

        var nested = Scope(
            "sub",
            body,
            Immediate("chargeCard", log),
            Immediate("reserveSeat", log),
            Blocking("fraudCheck", log),
            Immediate("refundCard", log),
            Blocking("releaseSeat", log));

        return Scope("scope", definition, nested, Immediate("after", log), Immediate("unwind", log));
    }
}
