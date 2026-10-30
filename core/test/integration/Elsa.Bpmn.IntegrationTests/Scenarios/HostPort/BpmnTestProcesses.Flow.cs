using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.IntegrationTests.Scenarios.HostPort.Activities;
using Elsa.Workflows;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;

internal static partial class BpmnTestProcesses
{
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

    /// <summary>A parallel gateway split and join.</summary>
    public static BpmnProcess ParallelSplitAndJoin(BpmnTestLog log) =>
        ParallelSplitAndJoinTopology("parallel-split-and-join", Immediate("left", log), Immediate("right", log), log);

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
}
