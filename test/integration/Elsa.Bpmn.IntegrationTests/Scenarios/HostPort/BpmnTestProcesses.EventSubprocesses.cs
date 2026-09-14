using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.IntegrationTests.Scenarios.HostPort.Activities;
using Elsa.Workflows;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;

internal static partial class BpmnTestProcesses
{
    /// <summary>
    /// A task that fails, with a dormant error-triggered event subprocess in the same scope to catch it.
    /// </summary>
    /// <remarks>
    /// An error event subprocess arms nothing: it rides the same <c>FaultSignal</c> seam an error boundary event does,
    /// and the only thing that distinguishes it here is where the recovery work runs — inside a nested scope of its
    /// own, seeded at the body's error start event, rather than on an outbound flow of the enclosing graph.
    /// </remarks>
    public static BpmnProcess ErrorEventSubprocess(BpmnTestLog log)
    {
        var body = new BpmnProcessBuilder("error-event-subprocess-body")
            .Element(EventSubprocessStart("errStart", Error()))
            .Task("handleError", bindingRef: BindingRef("handleError"))
            .EndEvent("errEnd")
            .ConnectSequence("errStart", "handleError", "errEnd")
            .Build();

        var definition = new BpmnProcessBuilder("error-event-subprocess")
            .StartEvent("start")
            .Task("risky", bindingRef: BindingRef("risky"))
            .Task("after", bindingRef: BindingRef("after"))
            .EndEvent("end")
            .Element(EventSubprocess("evtSub"))
            .ConnectSequence("start", "risky", "after", "end")
            .Build();

        return Scope("scope", definition, Faulting("risky", log), Immediate("after", log), Scope("evtSub", body, Immediate("handleError", log)));
    }

    /// <summary>
    /// An escalation thrown out of an embedded subprocess, caught by a non-interrupting escalation-triggered event
    /// subprocess on the enclosing scope rather than by a boundary event on the subprocess.
    /// </summary>
    /// <remarks>
    /// Non-interrupting, so the escalating subprocess keeps running and nothing in the scope is torn down. That is
    /// also what makes "the scope-level catcher fired" distinguishable from "the subprocess was stopped": with an
    /// interrupting catcher the two are the same observation.
    /// </remarks>
    public static BpmnProcess EscalationEventSubprocessOutOfSubprocess(BpmnTestLog log)
    {
        var subBody = new BpmnProcessBuilder("escalating-subprocess-body")
            .StartEvent("subStart")
            .Task("subWork", bindingRef: BindingRef("subWork"))
            .IntermediateThrowEvent("subEscalate", Escalation("REVIEW"))
            .Task("subMore", bindingRef: BindingRef("subMore"))
            .EndEvent("subEnd")
            .ConnectSequence("subStart", "subWork", "subEscalate", "subMore", "subEnd")
            .Build();

        var handlerBody = new BpmnProcessBuilder("escalation-event-subprocess-body")
            .Element(EventSubprocessStart("escStart", Escalation("REVIEW"), interrupting: false))
            .Task("handleEscalation", bindingRef: BindingRef("handleEscalation"))
            .EndEvent("escEnd")
            .ConnectSequence("escStart", "handleEscalation", "escEnd")
            .Build();

        var definition = new BpmnProcessBuilder("escalation-event-subprocess")
            .StartEvent("start")
            .SubProcess("sub", bindingRef: BindingRef("sub"))
            .Task("after", bindingRef: BindingRef("after"))
            .EndEvent("end")
            .Element(EventSubprocess("evtSub"))
            .ConnectSequence("start", "sub", "after", "end")
            .Build();

        var nested = Scope("sub", subBody, Blocking("subWork", log), Blocking("subMore", log));

        return Scope("scope", definition, nested, Immediate("after", log), Scope("evtSub", handlerBody, Immediate("handleEscalation", log)));
    }

    /// <summary>
    /// A non-interrupting message-triggered event subprocess: a listener armed at scope start, and a body that runs
    /// each time the listener fires while the scope's own long-running work is still going.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The listener is the second binding channel — <c>listenerBindingRef</c> — and is bound in the same
    /// <c>WorkBindings</c> map as everything else. It stands in for a real message wait: blocking work a test
    /// finishes, which is exactly what "the trigger fired" means to the host.
    /// </para>
    /// <para>
    /// <c>work</c> blocks so the scope stays open across the fires and so that when it finally completes, the armed
    /// listener is a <em>running</em> activity rather than a scheduled-but-not-yet-invoked one — the second of which
    /// this host cannot withdraw at all.
    /// </para>
    /// </remarks>
    public static BpmnProcess MessageEventSubprocess(BpmnTestLog log)
    {
        var body = new BpmnProcessBuilder("message-event-subprocess-body")
            .Element(EventSubprocessStart("msgStart", Message("nudge"), interrupting: false))
            .Task("handleNudge", bindingRef: BindingRef("handleNudge"))
            .EndEvent("msgEnd")
            .ConnectSequence("msgStart", "handleNudge", "msgEnd")
            .Build();

        var definition = new BpmnProcessBuilder("message-event-subprocess")
            .StartEvent("start")
            .Task("work", bindingRef: BindingRef("work"))
            .EndEvent("end")
            .Element(EventSubprocess("evtSub", listenerBindingRef: BindingRef("nudgeListener")))
            .ConnectSequence("start", "work", "end")
            .Build();

        return Scope(
            "scope",
            definition,
            Blocking("work", log),
            Blocking("nudgeListener", log),
            Scope("evtSub", body, Immediate("handleNudge", log)));
    }

    /// <summary>
    /// The same message-triggered event subprocess, but inside an embedded subprocess that completes while the
    /// enclosing scope carries on — so a listener that outlived the scope that armed it is distinguishable from one
    /// that merely outlived the workflow.
    /// </summary>
    /// <remarks>
    /// At the root, "the armed work does not survive the scope" and "does not survive the workflow" are the same
    /// observation, and Elsa tears a finished workflow's children down regardless. Here the workflow keeps running
    /// after the scope that armed the listener has completed, which is the only shape in which a listener left behind
    /// is a listener something could still resume into.
    /// </remarks>
    public static BpmnProcess NestedMessageEventSubprocess(BpmnTestLog log)
    {
        var handlerBody = new BpmnProcessBuilder("nested-message-event-subprocess-body")
            .Element(EventSubprocessStart("msgStart", Message("nudge"), interrupting: false))
            .Task("handleNudge", bindingRef: BindingRef("handleNudge"))
            .EndEvent("msgEnd")
            .ConnectSequence("msgStart", "handleNudge", "msgEnd")
            .Build();

        var subBody = new BpmnProcessBuilder("listening-subprocess-body")
            .StartEvent("subStart")
            .Task("subWork", bindingRef: BindingRef("subWork"))
            .EndEvent("subEnd")
            .Element(EventSubprocess("evtSub", listenerBindingRef: BindingRef("nudgeListener")))
            .ConnectSequence("subStart", "subWork", "subEnd")
            .Build();

        var definition = new BpmnProcessBuilder("nested-message-event-subprocess")
            .StartEvent("start")
            .SubProcess("sub", bindingRef: BindingRef("sub"))
            .Task("after", bindingRef: BindingRef("after"))
            .EndEvent("end")
            .ConnectSequence("start", "sub", "after", "end")
            .Build();

        var nested = Scope(
            "sub",
            subBody,
            Blocking("subWork", log),
            Blocking("nudgeListener", log),
            Scope("evtSub", handlerBody, Immediate("handleNudge", log)));

        return Scope("scope", definition, nested, Immediate("after", log));
    }

    /// <summary>
    /// An error-triggered event subprocess whose body runs an ordinary embedded subprocess of its own, so the
    /// start-element hint has both a place to arrive and a place it must not reach.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The body's only start event is event-defined, which is what makes the hint's arrival observable rather than
    /// merely asserted: seeded from the hint the body runs, and seeded as an ordinary direct invocation it faults
    /// deterministically with <c>bpmn.start.none-available</c>, because there is no none start event to begin at.
    /// </para>
    /// <para>
    /// The nested <c>inner</c> subprocess is the other direction. Its own invocation carries an ordinary scheduling
    /// cause, so the hint must not be inherited: were it, the inner process would be seeded at an element it does not
    /// declare and fault with <c>bpmn.start.unresolved-hint</c> instead of starting at its own none start event.
    /// </para>
    /// </remarks>
    public static BpmnProcess EventSubprocessBodyWithNestedSubprocess(BpmnTestLog log)
    {
        var innerBody = new BpmnProcessBuilder("event-subprocess-inner-body")
            .StartEvent("innerStart")
            .Task("innerOnly", bindingRef: BindingRef("innerOnly"))
            .EndEvent("innerEnd")
            .ConnectSequence("innerStart", "innerOnly", "innerEnd")
            .Build();

        var body = new BpmnProcessBuilder("hinted-event-subprocess-body")
            .Element(EventSubprocessStart("errStart", Error()))
            .Task("handleError", bindingRef: BindingRef("handleError"))
            .SubProcess("inner", bindingRef: BindingRef("inner"))
            .EndEvent("errEnd")
            .ConnectSequence("errStart", "handleError", "inner", "errEnd")
            .Build();

        var definition = new BpmnProcessBuilder("event-subprocess-start-hint")
            .StartEvent("start")
            .Task("risky", bindingRef: BindingRef("risky"))
            .EndEvent("end")
            .Element(EventSubprocess("evtSub"))
            .ConnectSequence("start", "risky", "end")
            .Build();

        var handler = Scope("evtSub", body, Immediate("handleError", log), Scope("inner", innerBody, Immediate("innerOnly", log)));

        return Scope("scope", definition, Faulting("risky", log), handler);
    }

    /// <summary>An event subprocess whose body declares two start events, which the library refuses.</summary>
    public static BpmnProcess EventSubprocessBodyWithTwoStartEvents(BpmnTestLog log)
    {
        var body = new BpmnProcessBuilder("two-start-event-subprocess-body")
            .Element(EventSubprocessStart("errStart", Error()))
            .StartEvent("alsoStart")
            .Task("handleError", bindingRef: BindingRef("handleError"))
            .EndEvent("errEnd")
            .ConnectSequence("errStart", "handleError", "errEnd")
            .Connect("alsoStart", "handleError")
            .Build();

        return RefusedEventSubprocessScope("two-start-events", log, ("evtSub", body, "handleError"));
    }

    /// <summary>Two error-triggered event subprocesses in one scope, which the library refuses.</summary>
    public static BpmnProcess TwoErrorEventSubprocesses(BpmnTestLog log)
    {
        BpmnProcessDefinition Body(string prefix) => new BpmnProcessBuilder($"{prefix}-error-event-subprocess-body")
            .Element(EventSubprocessStart($"{prefix}Start", Error()))
            .Task($"{prefix}Handle", bindingRef: BindingRef($"{prefix}Handle"))
            .EndEvent($"{prefix}End")
            .ConnectSequence($"{prefix}Start", $"{prefix}Handle", $"{prefix}End")
            .Build();

        return RefusedEventSubprocessScope(
            "two-error-event-subprocesses",
            log,
            ("evtSubA", Body("first"), "firstHandle"),
            ("evtSubB", Body("second"), "secondHandle"));
    }

    /// <summary>Two code-less catch-all escalation-triggered event subprocesses in one scope, which the library refuses.</summary>
    public static BpmnProcess TwoCatchAllEscalationEventSubprocesses(BpmnTestLog log)
    {
        BpmnProcessDefinition Body(string prefix) => new BpmnProcessBuilder($"{prefix}-escalation-event-subprocess-body")
            .Element(EventSubprocessStart($"{prefix}Start", Escalation(), interrupting: false))
            .Task($"{prefix}Handle", bindingRef: BindingRef($"{prefix}Handle"))
            .EndEvent($"{prefix}End")
            .ConnectSequence($"{prefix}Start", $"{prefix}Handle", $"{prefix}End")
            .Build();

        return RefusedEventSubprocessScope(
            "two-catch-all-escalation-event-subprocesses",
            log,
            ("evtSubA", Body("first"), "firstHandle"),
            ("evtSubB", Body("second"), "secondHandle"));
    }

    /// <summary>A non-interrupting error-triggered event subprocess, which is not legal BPMN and which the library refuses.</summary>
    public static BpmnProcess NonInterruptingErrorEventSubprocess(BpmnTestLog log)
    {
        var body = new BpmnProcessBuilder("non-interrupting-error-event-subprocess-body")
            .Element(EventSubprocessStart("errStart", Error(), interrupting: false))
            .Task("handleError", bindingRef: BindingRef("handleError"))
            .EndEvent("errEnd")
            .ConnectSequence("errStart", "handleError", "errEnd")
            .Build();

        return RefusedEventSubprocessScope("non-interrupting-error-event-subprocess", log, ("evtSub", body, "handleError"));
    }

    /// <summary>
    /// The <c>start/only/end</c> graph the refusal processes share, carrying the event subprocesses whose declaration
    /// the library refuses. Nothing in it ever runs: the refusal is raised when the scope builds its graph, which is
    /// before any work is started.
    /// </summary>
    private static BpmnProcess RefusedEventSubprocessScope(string processId, BpmnTestLog log, params (string ElementId, BpmnProcessDefinition Body, string HandlerId)[] eventSubprocesses)
    {
        var builder = new BpmnProcessBuilder(processId)
            .StartEvent("start")
            .Task("only", bindingRef: BindingRef("only"))
            .EndEvent("end")
            .ConnectSequence("start", "only", "end");

        foreach (var eventSubprocess in eventSubprocesses)
            builder = builder.Element(EventSubprocess(eventSubprocess.ElementId));

        var work = new List<IActivity> { Immediate("only", log) };

        work.AddRange(eventSubprocesses.Select(eventSubprocess => Scope(eventSubprocess.ElementId, eventSubprocess.Body, Immediate(eventSubprocess.HandlerId, log))));

        return Scope("scope", builder.Build(), work.ToArray());
    }
}
