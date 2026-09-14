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
/// the same way in the model and in the assertions. The processes themselves live in sibling partials, one per BPMN
/// construct family; this file holds the element factories and scope builders they all share.
/// </remarks>
internal static partial class BpmnTestProcesses
{
    /// <summary>The binding ref the given element's work is declared under.</summary>
    public static string BindingRef(string elementId) => $"node-{elementId}";

    private static BpmnEventDefinition Timer() => new(BpmnEventDefinitionTypes.Timer);

    private static BpmnEventDefinition Cancel() => new(BpmnEventDefinitionTypes.Cancel);

    private static BpmnEventDefinition Compensation(string? activityRef = null) =>
        activityRef is null
            ? new(BpmnEventDefinitionTypes.Compensation)
            : new(BpmnEventDefinitionTypes.Compensation, new Dictionary<string, string>(StringComparer.Ordinal) { [BpmnEventDefinitionProperties.ActivityRef] = activityRef });

    /// <summary>
    /// A compensation boundary event: dormant, arming nothing, and reaching its handler by association rather than by
    /// a sequence flow. <see cref="BpmnProcessBuilder.BoundaryEvent"/> cannot carry the handler association, so this
    /// one is written out.
    /// </summary>
    private static BpmnElement CompensationBoundary(string elementId, string attachedTo, string handler) =>
        new(elementId,
            BpmnElementTypes.BoundaryEvent,
            attachedToRef: attachedTo,
            eventDefinitions: [Compensation()],
            compensationHandlerElementId: handler);

    /// <summary>
    /// A compensation handler: a task that binds work like any other, takes no sequence flows, and is invoked only by
    /// compensation replay.
    /// </summary>
    private static BpmnElement CompensationHandler(string elementId) =>
        new(elementId, BpmnElementTypes.Task, bindingRef: BindingRef(elementId), isForCompensation: true);

    private static BpmnEventDefinition Escalation(string? code = null) =>
        code is null
            ? new(BpmnEventDefinitionTypes.Escalation)
            : new(BpmnEventDefinitionTypes.Escalation, new Dictionary<string, string>(StringComparer.Ordinal) { [BpmnEventDefinitionProperties.Code] = code });

    private static BpmnEventDefinition Error() => new(BpmnEventDefinitionTypes.Error);

    private static BpmnEventDefinition Message(string name) =>
        new(BpmnEventDefinitionTypes.Message, new Dictionary<string, string>(StringComparer.Ordinal) { [BpmnEventDefinitionProperties.Name] = name });

    /// <summary>
    /// An event subprocess: a flow-less subprocess whose bound work is the body, activated by that body's single
    /// start event rather than by a sequence flow. <see cref="BpmnProcessBuilder.SubProcess"/> carries the
    /// <c>triggeredByEvent</c> flag but not the listener binding, so this one is written out.
    /// </summary>
    private static BpmnElement EventSubprocess(string elementId, string? listenerBindingRef = null) =>
        new(elementId,
            BpmnElementTypes.SubProcess,
            bindingRef: BindingRef(elementId),
            triggeredByEvent: true,
            listenerBindingRef: listenerBindingRef);

    /// <summary>
    /// An event subprocess body's single start event, carrying its trigger and its <c>isInterrupting</c> flag.
    /// <see cref="BpmnProcessBuilder.StartEvent"/> cannot carry the flag, so this one is written out.
    /// </summary>
    private static BpmnElement EventSubprocessStart(string elementId, BpmnEventDefinition trigger, bool interrupting = true) =>
        new(elementId, BpmnElementTypes.StartEvent, eventDefinitions: [trigger], cancelActivity: interrupting);

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
