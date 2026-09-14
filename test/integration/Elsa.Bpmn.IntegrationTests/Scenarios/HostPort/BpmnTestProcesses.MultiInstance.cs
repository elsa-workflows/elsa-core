using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.IntegrationTests.Scenarios.HostPort.Activities;
using Elsa.Workflows.Memory;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;

internal static partial class BpmnTestProcesses
{
    /// <summary>The name of the variable <see cref="CollectionMultiInstanceTask"/> loops over.</summary>
    public const string CollectionVariableName = "items";

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
}
