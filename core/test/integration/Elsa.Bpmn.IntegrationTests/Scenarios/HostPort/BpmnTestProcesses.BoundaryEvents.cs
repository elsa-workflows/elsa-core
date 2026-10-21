using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.IntegrationTests.Scenarios.HostPort.Activities;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;

internal static partial class BpmnTestProcesses
{
    /// <summary>An interrupting timer boundary event on a long-running task.</summary>
    public static BpmnProcess InterruptingTimerBoundary(BpmnTestLog log)
    {
        var definition = new BpmnProcessBuilder("interrupting-timer-boundary")
            .StartEvent("start")
            .Task("task", bindingRef: BindingRef("task"))
            .EndEvent("end")
            .BoundaryEvent("timeout", attachedTo: "task", eventDefinition: Timer(), interrupting: true, bindingRef: BindingRef("timeout"))
            .Task("onTimeout", bindingRef: BindingRef("onTimeout"))
            .EndEvent("timedOut")
            .ConnectSequence("start", "task", "end")
            .ConnectSequence("timeout", "onTimeout", "timedOut")
            .Build();

        return Scope("scope", definition, Blocking("task", log), Blocking("timeout", log), Immediate("onTimeout", log));
    }

    /// <summary>A task that fails, with an error boundary event that catches it.</summary>
    public static BpmnProcess ErrorBoundaryCaught(BpmnTestLog log)
    {
        var definition = new BpmnProcessBuilder("error-boundary-caught")
            .StartEvent("start")
            .Task("risky", bindingRef: BindingRef("risky"))
            .EndEvent("end")
            .BoundaryEvent("oops", attachedTo: "risky", eventDefinition: new BpmnEventDefinition(BpmnEventDefinitionTypes.Error))
            .Task("recover", bindingRef: BindingRef("recover"))
            .EndEvent("recovered")
            .ConnectSequence("start", "risky", "end")
            .ConnectSequence("oops", "recover", "recovered")
            .Build();

        return Scope("scope", definition, Faulting("risky", log), Immediate("recover", log));
    }

    /// <summary>
    /// A task that fails inside an embedded subprocess with nothing there to catch it, and an error boundary event on
    /// the subprocess in the enclosing scope that does.
    /// </summary>
    public static BpmnProcess ErrorPropagatedOutOfSubprocess(BpmnTestLog log)
    {
        var body = new BpmnProcessBuilder("failing-subprocess-body")
            .StartEvent("subStart")
            .Task("subRisky", bindingRef: BindingRef("subRisky"))
            .EndEvent("subEnd")
            .ConnectSequence("subStart", "subRisky", "subEnd")
            .Build();

        var definition = new BpmnProcessBuilder("error-propagated-out-of-subprocess")
            .StartEvent("start")
            .SubProcess("sub", bindingRef: BindingRef("sub"))
            .Task("after", bindingRef: BindingRef("after"))
            .EndEvent("end")
            .BoundaryEvent("subOops", attachedTo: "sub", eventDefinition: new BpmnEventDefinition(BpmnEventDefinitionTypes.Error))
            .Task("subRecover", bindingRef: BindingRef("subRecover"))
            .EndEvent("subRecovered")
            .ConnectSequence("start", "sub", "after", "end")
            .ConnectSequence("subOops", "subRecover", "subRecovered")
            .Build();

        var nested = Scope("sub", body, Faulting("subRisky", log));

        return Scope("scope", definition, nested, Immediate("after", log), Immediate("subRecover", log));
    }

    /// <summary>
    /// A task that fails inside an embedded subprocess, with an error boundary event on the task inside the body. The
    /// root runs two tasks before the subprocess, so the subprocess's own tokens and the root's are numbered apart: a
    /// catcher found through the root's token instead of the body's would miss (valence-works/bpmn#32).
    /// </summary>
    public static BpmnProcess ErrorBoundaryInsideSubprocess(BpmnTestLog log)
    {
        var body = new BpmnProcessBuilder("error-boundary-inside-subprocess-body")
            .StartEvent("subStart")
            .Task("subRisky", bindingRef: BindingRef("subRisky"))
            .EndEvent("subEnd")
            .BoundaryEvent("subRiskyOops", attachedTo: "subRisky", eventDefinition: new BpmnEventDefinition(BpmnEventDefinitionTypes.Error))
            .Task("subRecover", bindingRef: BindingRef("subRecover"))
            .EndEvent("subRecovered")
            .ConnectSequence("subStart", "subRisky", "subEnd")
            .ConnectSequence("subRiskyOops", "subRecover", "subRecovered")
            .Build();

        var definition = new BpmnProcessBuilder("error-boundary-inside-subprocess")
            .StartEvent("start")
            .Task("first", bindingRef: BindingRef("first"))
            .Task("second", bindingRef: BindingRef("second"))
            .SubProcess("sub", bindingRef: BindingRef("sub"))
            .Task("after", bindingRef: BindingRef("after"))
            .EndEvent("end")
            .ConnectSequence("start", "first", "second", "sub", "after", "end")
            .Build();

        var nested = Scope("sub", body, Faulting("subRisky", log), Immediate("subRecover", log));

        return Scope("scope", definition, Immediate("first", log), Immediate("second", log), nested, Immediate("after", log));
    }

    /// <summary>
    /// A task that fails with the error code <paramref name="code"/>, carrying an error boundary event for
    /// <paramref name="boundaryCode"/> and, when <paramref name="withCatchAll"/>, a catch-all one as well.
    /// </summary>
    public static BpmnProcess CodedErrorBoundaries(BpmnTestLog log, string? code, string boundaryCode, bool withCatchAll)
    {
        var builder = new BpmnProcessBuilder("coded-error-boundaries")
            .StartEvent("start")
            .Task("risky", bindingRef: BindingRef("risky"))
            .EndEvent("end")
            .BoundaryEvent("coded", attachedTo: "risky", eventDefinition: Error(boundaryCode))
            .Task("handleCoded", bindingRef: BindingRef("handleCoded"))
            .EndEvent("codedHandled")
            .ConnectSequence("start", "risky", "end")
            .ConnectSequence("coded", "handleCoded", "codedHandled");

        if (withCatchAll)
        {
            builder = builder
                .BoundaryEvent("any", attachedTo: "risky", eventDefinition: Error())
                .Task("handleAny", bindingRef: BindingRef("handleAny"))
                .EndEvent("anyHandled")
                .ConnectSequence("any", "handleAny", "anyHandled");
        }

        return Scope("scope", builder.Build(), Faulting("risky", log, code), Immediate("handleCoded", log), Immediate("handleAny", log));
    }

    /// <summary>
    /// An embedded subprocess whose body ends at an error end event for <paramref name="code"/>, with nothing in the body
    /// to catch it, and an error boundary event for the same code on the subprocess in the enclosing scope.
    /// </summary>
    public static BpmnProcess ErrorEndEventCaughtByTheEnclosingScope(BpmnTestLog log, string code)
    {
        var body = new BpmnProcessBuilder("error-end-event-body")
            .StartEvent("subStart")
            .Task("subWork", bindingRef: BindingRef("subWork"))
            .EndEvent("subFailed", null, Error(code))
            .ConnectSequence("subStart", "subWork", "subFailed")
            .Build();

        var definition = new BpmnProcessBuilder("error-end-event-caught-by-the-enclosing-scope")
            .StartEvent("start")
            .SubProcess("sub", bindingRef: BindingRef("sub"))
            .Task("after", bindingRef: BindingRef("after"))
            .EndEvent("end")
            .BoundaryEvent("subOops", attachedTo: "sub", eventDefinition: Error(code))
            .Task("subRecover", bindingRef: BindingRef("subRecover"))
            .EndEvent("subRecovered")
            .ConnectSequence("start", "sub", "after", "end")
            .ConnectSequence("subOops", "subRecover", "subRecovered")
            .Build();

        var nested = Scope("sub", body, Immediate("subWork", log));

        return Scope("scope", definition, nested, Immediate("after", log), Immediate("subRecover", log));
    }

    /// <summary>A task that fails with nothing to catch it.</summary>
    public static BpmnProcess UncaughtError(BpmnTestLog log)
    {
        var definition = new BpmnProcessBuilder("uncaught-error")
            .StartEvent("start")
            .Task("risky", bindingRef: BindingRef("risky"))
            .EndEvent("end")
            .ConnectSequence("start", "risky", "end")
            .Build();

        return Scope("scope", definition, Faulting("risky", log));
    }

    /// <summary>
    /// An escalation thrown out of an embedded subprocess, caught by a non-interrupting escalation boundary event on
    /// the subprocess. The nested scope keeps running, which is what "non-interrupting" means.
    /// </summary>
    public static BpmnProcess EscalationOutOfSubprocess(BpmnTestLog log)
    {
        // subFirst runs before anything the parent could be confused with, deliberately: it puts the nested scope's
        // handle counter ahead of the parent's. A host that recognised its work by a shared, rewritable key rather than
        // by the child activity execution would otherwise be rescued by two independent counters happening to agree.
        var body = new BpmnProcessBuilder("subprocess-body")
            .StartEvent("subStart")
            .Task("subFirst", bindingRef: BindingRef("subFirst"))
            .Task("subWork", bindingRef: BindingRef("subWork"))
            .IntermediateThrowEvent("subEscalate", Escalation("REVIEW"))
            .Task("subMore", bindingRef: BindingRef("subMore"))
            .EndEvent("subEnd")
            .ConnectSequence("subStart", "subFirst", "subWork", "subEscalate", "subMore", "subEnd")
            .Build();

        var definition = new BpmnProcessBuilder("escalation-out-of-subprocess")
            .StartEvent("start")
            .SubProcess("sub", bindingRef: BindingRef("sub"))
            .Task("after", bindingRef: BindingRef("after"))
            .EndEvent("end")
            .BoundaryEvent("escalated", attachedTo: "sub", eventDefinition: Escalation("REVIEW"), interrupting: false)
            .Task("notify", bindingRef: BindingRef("notify"))
            .EndEvent("notified")
            .ConnectSequence("start", "sub", "after", "end")
            .ConnectSequence("escalated", "notify", "notified")
            .Build();

        // subMore blocks so the subprocess is still running when the escalation path executes, which is what makes
        // "the escalating work is still live" observable rather than merely asserted.
        var nested = Scope("sub", body, Immediate("subFirst", log), Blocking("subWork", log), Blocking("subMore", log));

        return Scope("scope", definition, nested, Immediate("after", log), Immediate("notify", log));
    }

    /// <summary>
    /// A message or signal boundary event on a long-running task, interrupting or not, each path leading to its own
    /// work.
    /// </summary>
    /// <remarks>
    /// The boundary's listener is blocking work a test finishes, which is what "the message arrived" or "the signal was
    /// broadcast" means to the host. The task blocks too, so the boundary fires while its host is still running and the
    /// difference between interrupting and non-interrupting is whether that host survives the fire.
    /// </remarks>
    private static BpmnProcess CatchBoundary(BpmnTestLog log, BpmnEventDefinition trigger, bool interrupting)
    {
        var definition = new BpmnProcessBuilder("catch-boundary")
            .StartEvent("start")
            .Task("task", bindingRef: BindingRef("task"))
            .Task("afterTask", bindingRef: BindingRef("afterTask"))
            .EndEvent("end")
            .BoundaryEvent("caught", attachedTo: "task", eventDefinition: trigger, interrupting: interrupting, bindingRef: BindingRef("caught"))
            .Task("onCaught", bindingRef: BindingRef("onCaught"))
            .EndEvent("caughtEnd")
            .ConnectSequence("start", "task", "afterTask", "end")
            .ConnectSequence("caught", "onCaught", "caughtEnd")
            .Build();

        return Scope("scope", definition, Blocking("task", log), Immediate("afterTask", log), Blocking("caught", log), Immediate("onCaught", log));
    }

    /// <summary>A message boundary event on a long-running task. See <see cref="CatchBoundary"/>.</summary>
    public static BpmnProcess MessageBoundary(BpmnTestLog log, bool interrupting) => CatchBoundary(log, Message("OrderAmended"), interrupting);

    /// <summary>A signal boundary event on a long-running task. See <see cref="CatchBoundary"/>.</summary>
    public static BpmnProcess SignalBoundary(BpmnTestLog log, bool interrupting) => CatchBoundary(log, Signal("RecallIssued"), interrupting);
}
