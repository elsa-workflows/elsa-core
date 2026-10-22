using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.IntegrationTests.Scenarios.HostPort.Activities;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort;

internal static partial class BpmnTestProcesses
{
    private static BpmnEventDefinition Terminate() => new(BpmnEventDefinitionTypes.Terminate);

    /// <summary>
    /// A parallel split whose first branch starts long-running work and whose second branch goes straight to a
    /// terminate end event. Both tokens leave the split in the scope's start evaluation, in emission order, so
    /// <c>work</c> is activated first and the terminate is reached second, in the same evaluation.
    /// </summary>
    public static BpmnProcess TerminateInTheEvaluationThatStartsWork(BpmnTestLog log)
    {
        var definition = new BpmnProcessBuilder("terminate-in-the-evaluation-that-starts-work")
            .StartEvent("start")
            .ParallelGateway("split")
            .Task("work", bindingRef: BindingRef("work"))
            .EndEvent("workEnd")
            .EndEvent("kill", eventDefinitions: [Terminate()])
            .ConnectSequence("start", "split")
            .Connect("split", "work")
            .Connect("split", "kill")
            .Connect("work", "workEnd")
            .Build();

        return Scope("scope", definition, Blocking("work", log));
    }

    /// <summary>
    /// The same live sibling as <see cref="TerminateInTheEvaluationThatStartsWork"/>, but the terminate end event is
    /// reached only once <c>gate</c> is resumed, in an evaluation that starts no new work.
    /// </summary>
    public static BpmnProcess TerminateInAnEvaluationThatStartsNoWork(BpmnTestLog log)
    {
        var definition = new BpmnProcessBuilder("terminate-in-an-evaluation-that-starts-no-work")
            .StartEvent("start")
            .ParallelGateway("split")
            .Task("work", bindingRef: BindingRef("work"))
            .EndEvent("workEnd")
            .Task("gate", bindingRef: BindingRef("gate"))
            .EndEvent("kill", eventDefinitions: [Terminate()])
            .ConnectSequence("start", "split")
            .Connect("split", "work")
            .Connect("split", "gate")
            .Connect("work", "workEnd")
            .Connect("gate", "kill")
            .Build();

        return Scope("scope", definition, Blocking("work", log), Blocking("gate", log));
    }

    /// <summary>
    /// A parallel split whose first branch starts long-running work and whose second branch reaches an exclusive
    /// gateway that can never choose: it binds no decision, declares no default, and both of its flows are
    /// conditional. The gateway raises a behavior fault in the scope's start evaluation, after <c>work</c> was
    /// activated in that same evaluation.
    /// </summary>
    public static BpmnProcess FaultInTheEvaluationThatStartsWork(BpmnTestLog log)
    {
        var definition = new BpmnProcessBuilder("fault-in-the-evaluation-that-starts-work")
            .StartEvent("start")
            .ParallelGateway("split")
            .Task("work", bindingRef: BindingRef("work"))
            .EndEvent("workEnd")
            .ExclusiveGateway("undecidable")
            .EndEvent("left")
            .EndEvent("right")
            .ConnectSequence("start", "split")
            .Connect("split", "work")
            .Connect("split", "undecidable")
            .Connect("work", "workEnd")
            .Connect("undecidable", "left", condition: "Left")
            .Connect("undecidable", "right", condition: "Right")
            .Build();

        return Scope("scope", definition, Blocking("work", log));
    }
}
