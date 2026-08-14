using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.IntegrationTests.Scenarios.HostPort.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Memory;

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
    public static BpmnProcess ParallelSplitAndJoin(BpmnTestLog log) =>
        ParallelSplitAndJoinTopology("parallel-split-and-join", Immediate("left", log), Immediate("right", log), log);

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

    /// <summary>
    /// A sequential multi-instance task: one instance at a time, each blocking until a test finishes it. Used to
    /// drive many evaluations of one scope so a persisted blob that is not pruned is observable as unbounded growth.
    /// </summary>
    public static BpmnProcess SequentialMultiInstanceTask(BpmnTestLog log, int cardinality)
    {
        var definition = new BpmnProcessBuilder("sequential-multi-instance-task")
            .StartEvent("start")
            .Task(BpmnElementTypes.Task, "each", bindingRef: BindingRef("each"), loopCharacteristics: new BpmnLoopCharacteristics(isSequential: true, cardinality: cardinality))
            .Task("after", bindingRef: BindingRef("after"))
            .EndEvent("end")
            .ConnectSequence("start", "each", "after", "end")
            .Build();

        return Scope("scope", definition, Blocking("each", log), Immediate("after", log));
    }

    /// <summary>
    /// A parallel split into two branches that both block, and a join. Used to prove a scope suspends with two live
    /// units of work outstanding and, once resumed, matches each completion back to its own binding through the
    /// rehydrated ledger.
    /// </summary>
    public static BpmnProcess ParallelSplitAndJoinBlocking(BpmnTestLog log) =>
        ParallelSplitAndJoinTopology("parallel-split-and-join-blocking", Blocking("left", log), Blocking("right", log), log);

    /// <summary>
    /// The start/split/left/right/join/after/end graph shared by <see cref="ParallelSplitAndJoin"/> and
    /// <see cref="ParallelSplitAndJoinBlocking"/>, parameterised by the work the two branches run.
    /// </summary>
    private static BpmnProcess ParallelSplitAndJoinTopology(string processId, IActivity leftWork, IActivity rightWork, BpmnTestLog log)
    {
        var definition = new BpmnProcessBuilder(processId)
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

        return Scope("scope", definition, leftWork, rightWork, Immediate("after", log));
    }

    /// <summary>
    /// A collection-mode multi-instance task: one instance per item of a container-scoped variable, which the
    /// interpreter reads back through <c>IBpmnVariableReader</c> while it evaluates.
    /// </summary>
    /// <remarks>
    /// Three items rather than two, so the instance count cannot be confused with a declared cardinality. The
    /// collection variable is declared on both sides — on the definition, because <c>BpmnGraph.Build</c> refuses a
    /// loop naming a variable the process does not declare, and on the activity, because that is where the value
    /// actually lives.
    /// </remarks>
    public static BpmnProcess CollectionMultiInstanceTask(BpmnTestLog log)
    {
        var definition = new BpmnProcessBuilder("collection-multi-instance-task")
            .Variable(CollectionVariableName)
            .StartEvent("start")
            .Task(BpmnElementTypes.Task, "each", bindingRef: BindingRef("each"), loopCharacteristics: new BpmnLoopCharacteristics(isSequential: false, collectionVariable: CollectionVariableName))
            .Task("after", bindingRef: BindingRef("after"))
            .EndEvent("end")
            .ConnectSequence("start", "each", "after", "end")
            .Build();

        return Scope("scope", definition, [new Variable<string[]>(CollectionVariableName, ["alpha", "beta", "gamma"])], Immediate("each", log), Immediate("after", log));
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

    /// <summary>An embedded subprocess with one task in it, and one task after it in the enclosing scope.</summary>
    public static BpmnProcess NestedSubprocess(BpmnTestLog log)
    {
        var body = new BpmnProcessBuilder("nested-subprocess-body")
            .StartEvent("subStart")
            .Task("subOnly", bindingRef: BindingRef("subOnly"))
            .EndEvent("subEnd")
            .ConnectSequence("subStart", "subOnly", "subEnd")
            .Build();

        var definition = new BpmnProcessBuilder("nested-subprocess")
            .StartEvent("start")
            .SubProcess("sub", bindingRef: BindingRef("sub"))
            .Task("after", bindingRef: BindingRef("after"))
            .EndEvent("end")
            .ConnectSequence("start", "sub", "after", "end")
            .Build();

        return Scope("scope", definition, Scope("sub", body, Immediate("subOnly", log)), Immediate("after", log));
    }

    /// <summary>
    /// A parallel split and join, blocking on both branches, nested inside an embedded subprocess. Used to prove
    /// a <em>nested</em> scope's own ledger -- not just a root scope's -- matches each completion back to its
    /// binding after a round trip through Elsa's own serializer.
    /// </summary>
    public static BpmnProcess NestedParallelSplitAndJoinBlocking(BpmnTestLog log)
    {
        var body = new BpmnProcessBuilder("nested-parallel-split-and-join-body")
            .StartEvent("subStart")
            .ParallelGateway("subSplit")
            .Task("subLeft", bindingRef: BindingRef("subLeft"))
            .Task("subRight", bindingRef: BindingRef("subRight"))
            .ParallelGateway("subJoin")
            .Task("subAfter", bindingRef: BindingRef("subAfter"))
            .EndEvent("subEnd")
            .ConnectSequence("subStart", "subSplit")
            .Connect("subSplit", "subLeft")
            .Connect("subSplit", "subRight")
            .Connect("subLeft", "subJoin")
            .Connect("subRight", "subJoin")
            .ConnectSequence("subJoin", "subAfter", "subEnd")
            .Build();

        var definition = new BpmnProcessBuilder("nested-parallel-split-and-join-blocking")
            .StartEvent("start")
            .SubProcess("sub", bindingRef: BindingRef("sub"))
            .Task("after", bindingRef: BindingRef("after"))
            .EndEvent("end")
            .ConnectSequence("start", "sub", "after", "end")
            .Build();

        var nested = Scope("sub", body, Blocking("subLeft", log), Blocking("subRight", log), Immediate("subAfter", log));

        return Scope("scope", definition, nested, Immediate("after", log));
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

    /// <summary>The name of the variable <see cref="CollectionMultiInstanceTask"/> loops over.</summary>
    public const string CollectionVariableName = "items";

    private static BpmnEventDefinition Timer() => new(BpmnEventDefinitionTypes.Timer);

    private static BpmnEventDefinition Cancel() => new(BpmnEventDefinitionTypes.Cancel);

    private static BpmnEventDefinition Escalation(string code) =>
        new(BpmnEventDefinitionTypes.Escalation, new Dictionary<string, string>(StringComparer.Ordinal) { [BpmnEventDefinitionProperties.Code] = code });

    private static BpmnProcess Scope(string id, BpmnProcessDefinition definition, params IActivity[] work) => Scope(id, definition, [], work);

    private static BpmnProcess Scope(string id, BpmnProcessDefinition definition, Variable[] variables, params IActivity[] work) => new()
    {
        Id = id,
        Process = definition,
        Variables = variables.ToList(),
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
