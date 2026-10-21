using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.IntegrationTests.Scenarios.HostPort.Activities;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;

internal static partial class BpmnTestProcesses
{
    /// <summary>
    /// An inclusive split whose bound decision selects two of its three conditioned flows, and an inclusive join
    /// behind them.
    /// </summary>
    /// <remarks>
    /// Both selected branches block, so the join is observable while it has one of its two activated inbound flows and
    /// not the other: an inclusive join that waited for every inbound flow would never fire, because <c>middle</c> is
    /// never activated, and one that fired on the first arrival would run <c>after</c> before <c>right</c> finished.
    /// </remarks>
    public static BpmnProcess InclusiveSplitAndJoin(BpmnTestLog log)
    {
        var definition = new BpmnProcessBuilder("inclusive-split-and-join")
            .StartEvent("start")
            .Element(new BpmnElement("split", BpmnElementTypes.InclusiveGateway, bindingRef: BindingRef("split")))
            .Task("left", bindingRef: BindingRef("left"))
            .Task("middle", bindingRef: BindingRef("middle"))
            .Task("right", bindingRef: BindingRef("right"))
            .InclusiveGateway("join")
            .Task("after", bindingRef: BindingRef("after"))
            .EndEvent("end")
            .ConnectSequence("start", "split")
            .Connect("split", "left", condition: "left")
            .Connect("split", "middle", condition: "middle")
            .Connect("split", "right", condition: "right")
            .Connect("left", "join")
            .Connect("middle", "join")
            .Connect("right", "join")
            .ConnectSequence("join", "after", "end")
            .Build();

        return Scope(
            "scope",
            definition,
            Decision("split", log, "left", "right"),
            Blocking("left", log),
            Immediate("middle", log),
            Blocking("right", log),
            Immediate("after", log));
    }

    /// <summary>
    /// An event-based gateway racing a message catch against a timer catch, each leading to its own work.
    /// </summary>
    /// <remarks>
    /// Both catches are armed listeners -- blocking work a test finishes -- so either can be made to win, and the loser
    /// is a running activity the host has to withdraw rather than one that simply never started.
    /// </remarks>
    public static BpmnProcess EventBasedGatewayRace(BpmnTestLog log)
    {
        var definition = new BpmnProcessBuilder("event-based-gateway-race")
            .StartEvent("start")
            .EventBasedGateway("race")
            .IntermediateCatchEvent("payment", Message("PaymentReceived"), bindingRef: BindingRef("payment"))
            .IntermediateCatchEvent("deadline", Timer(), bindingRef: BindingRef("deadline"))
            .Task("ship", bindingRef: BindingRef("ship"))
            .EndEvent("shipped")
            .Task("cancelOrder", bindingRef: BindingRef("cancelOrder"))
            .EndEvent("cancelled")
            .ConnectSequence("start", "race")
            .Connect("race", "payment")
            .Connect("race", "deadline")
            .ConnectSequence("payment", "ship", "shipped")
            .ConnectSequence("deadline", "cancelOrder", "cancelled")
            .Build();

        return Scope(
            "scope",
            definition,
            Blocking("payment", log),
            Blocking("deadline", log),
            Immediate("ship", log),
            Immediate("cancelOrder", log));
    }
}
