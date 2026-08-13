using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.IntegrationTests.Scenarios.HostPort.Activities;
using Elsa.Workflows;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;

/// <summary>
/// The processes the applier is exercised against, built in code and bound to stand-in work activities.
/// </summary>
/// <remarks>
/// Every activity's id is the BPMN element id it runs, and its binding ref is that id prefixed, so a process reads
/// the same way in the model and in the assertions.
/// </remarks>
internal static class BpmnTestProcesses
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

    /// <summary>A parallel gateway split and join.</summary>
    public static BpmnProcess ParallelSplitAndJoin(BpmnTestLog log)
    {
        var definition = new BpmnProcessBuilder("parallel-split-and-join")
            .StartEvent("start")
            .ParallelGateway("split")
            .Task("left", bindingRef: BindingRef("left"))
            .Task("right", bindingRef: BindingRef("right"))
            .ParallelGateway("join")
            .Task("after", bindingRef: BindingRef("after"))
            .EndEvent("end")
            .ConnectSequence("start", "split")
            .Connect("split", "left")
            .Connect("split", "right")
            .Connect("left", "join")
            .Connect("right", "join")
            .ConnectSequence("join", "after", "end")
            .Build();

        return Scope("scope", definition, Immediate("left", log), Immediate("right", log), Immediate("after", log));
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
    /// A parallel multi-instance task: two concurrent instances of one binding, told apart only by their iteration id.
    /// </summary>
    public static BpmnProcess ParallelMultiInstanceTask(BpmnTestLog log)
    {
        var definition = new BpmnProcessBuilder("parallel-multi-instance-task")
            .StartEvent("start")
            .Task(BpmnElementTypes.Task, "each", bindingRef: BindingRef("each"), loopCharacteristics: new BpmnLoopCharacteristics(isSequential: false, cardinality: 2))
            .Task("after", bindingRef: BindingRef("after"))
            .EndEvent("end")
            .ConnectSequence("start", "each", "after", "end")
            .Build();

        return Scope("scope", definition, Blocking("each", log), Immediate("after", log));
    }

    /// <summary>A linear process: one task between a start and an end event.</summary>
    public static BpmnProcess LinearTask(BpmnTestLog log)
    {
        var definition = new BpmnProcessBuilder("linear-task")
            .StartEvent("start")
            .Task("only", bindingRef: BindingRef("only"))
            .EndEvent("end")
            .ConnectSequence("start", "only", "end")
            .Build();

        return Scope("scope", definition, Immediate("only", log));
    }

    /// <summary>The binding ref the given element's work is declared under.</summary>
    public static string BindingRef(string elementId) => $"node-{elementId}";

    private static BpmnEventDefinition Timer() => new(BpmnEventDefinitionTypes.Timer);

    private static BpmnEventDefinition Escalation(string code) =>
        new(BpmnEventDefinitionTypes.Escalation, new Dictionary<string, string>(StringComparer.Ordinal) { [BpmnEventDefinitionProperties.Code] = code });

    private static BpmnProcess Scope(string id, BpmnProcessDefinition definition, params IActivity[] work) => new()
    {
        Id = id,
        Process = definition,
        Activities = work.ToList(),
        WorkBindings = work.ToDictionary(activity => BindingRef(activity.Id), activity => activity.Id, StringComparer.Ordinal)
    };

    private static BpmnTestWork Immediate(string id, BpmnTestLog log) => new()
    {
        Id = id,
        Log = log
    };

    private static BpmnTestBlockingWork Blocking(string id, BpmnTestLog log) => new()
    {
        Id = id,
        Log = log
    };

    private static BpmnTestFaultingWork Faulting(string id, BpmnTestLog log) => new()
    {
        Id = id,
        Log = log
    };
}
