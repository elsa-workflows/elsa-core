using Bpmn.Interchange;
using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Notifications;

namespace Elsa.Bpmn.Interchange.Handlers.Notifications;

/// <summary>
/// The second net for <see cref="BpmnWorkBinding.UnboundTask"/>. <see cref="Binding.BpmnWorkBinder"/> already
/// refuses an unbound task at <em>import</em>, but a definition can be edited after import — through Elsa's own
/// workflow designer, not through the BPMN document — and an edit can remove the activity a task was bound to.
/// That edit never touches the BPMN document this scope still carries, so it is invisible to the bind-time
/// refusal; this handler is what catches it before the definition publishes.
/// </summary>
/// <remarks>
/// <para>
/// <b>Where the truth lives.</b> This inspects the materialized activity graph — each <see cref="BpmnProcess"/>'s
/// own <see cref="BpmnProcess.WorkBindings"/> and <see cref="Container.Activities"/> — not the BPMN source text a
/// document was imported from. The two can disagree, and only one of them is what a published workflow would
/// actually run: <see cref="BpmnProcess.Process"/> is an inert snapshot of the document as it read at import time,
/// so removing a bound activity from the graph after import leaves that snapshot, and any <c>elsa:activityBinding</c>
/// extension on it, exactly as it was. Checking the snapshot instead of the graph would make this gate pass on a
/// definition it exists to catch. Re-reading the document's stored source XML (kept only for BPMN export, and
/// already documented there as one a post-import edit can leave stale or remove outright) has the identical defect
/// for the identical reason: neither the snapshot nor the source text moves when a graph edit removes a binding, so
/// only the graph itself can answer whether the definition being published still has one. The graph is reached
/// through <see cref="IWorkflowGraphBuilder"/>, the same service <c>ValidateOutputConverters</c> in
/// <c>Elsa.Workflows.Management</c> uses for the same reason: it, not a hand-rolled walk of <c>Container.Activities</c>,
/// is what correctly reaches an activity nested through any composition shape a workflow can use, including a
/// <c>BpmnProcess</c> composed into a <c>Flowchart</c> (D11).
/// </para>
/// <para>
/// <b>Which elements this looks at.</b> Of every BPMN element kind that carries a <see cref="BpmnElement.BindingRef"/>,
/// only the task family — <see cref="BpmnElementTypes.Task"/>, <see cref="BpmnElementTypes.UserTask"/>,
/// <see cref="BpmnElementTypes.ServiceTask"/>, <see cref="BpmnElementTypes.ScriptTask"/>,
/// <see cref="BpmnElementTypes.ManualTask"/>, <see cref="BpmnElementTypes.BusinessRuleTask"/>,
/// <see cref="BpmnElementTypes.SendTask"/> and <see cref="BpmnElementTypes.ReceiveTask"/> — can be an
/// <see cref="BpmnWorkBinding.UnboundTask"/>; every other bindable kind (a call activity, a nested subprocess, a
/// message/signal/timer catch or throw, a message/signal/timer boundary) binds to an Elsa activity automatically and
/// takes no <c>elsa:activityBinding</c> declaration, so <see cref="Binding.BpmnWorkBinder"/> never refuses those for
/// want of one. A send or receive task that resolved a BPMN <c>message</c> is excluded the same way
/// <c>Bpmn.Interchange</c>'s own reader tells the two apart: it carries
/// <see cref="BpmnXmlReader.MessageNamePropertyKey"/> in its <see cref="BpmnElement.Properties"/>, which only that
/// resolution ever sets.
/// </para>
/// </remarks>
public class ValidateBpmnProcessBindings(IWorkflowGraphBuilder workflowGraphBuilder) : INotificationHandler<WorkflowDefinitionValidating>
{
    private static readonly IReadOnlySet<string> UnboundTaskElementTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        BpmnElementTypes.Task,
        BpmnElementTypes.UserTask,
        BpmnElementTypes.ServiceTask,
        BpmnElementTypes.ScriptTask,
        BpmnElementTypes.ManualTask,
        BpmnElementTypes.BusinessRuleTask,
        BpmnElementTypes.SendTask,
        BpmnElementTypes.ReceiveTask
    };

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionValidating notification, CancellationToken cancellationToken)
    {
        var graph = await workflowGraphBuilder.BuildAsync(notification.Workflow, cancellationToken);

        foreach (var node in graph.Nodes)
        {
            if (node.Activity is not BpmnProcess { Process: not null } scope)
                continue;

            foreach (var element in scope.Process!.Elements)
            {
                if (!IsUnboundTaskCandidate(element) || IsBound(scope, element.BindingRef!))
                    continue;

                notification.ValidationErrors.Add(new(
                    $"BPMN element '{element.ElementId}' ({element.ElementType}) of process '{scope.Process.ProcessId}' is not bound to an Elsa activity. "
                    + "BPMN does not say what this task does; declare an activity binding for it, or remove the element.",
                    element.ElementId));
            }
        }
    }

    private static bool IsUnboundTaskCandidate(BpmnElement element) =>
        element.BindingRef is not null
        && UnboundTaskElementTypes.Contains(element.ElementType)
        && !element.Properties.ContainsKey(BpmnXmlReader.MessageNamePropertyKey);

    private static bool IsBound(BpmnProcess scope, string bindingRef) =>
        scope.WorkBindings.TryGetValue(bindingRef, out var activityId)
        && scope.Activities.Any(activity => string.Equals(activity.Id, activityId, StringComparison.Ordinal));
}
