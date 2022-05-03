using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Runtime.Models;
using Elsa.State;

namespace Elsa.Runtime.Services;

/// <summary>
/// Invokes a workflow using a configured runtime (e.g. ProtoActor).
/// </summary>
public interface IWorkflowInvoker
{
    /// <summary>
    /// Creates a workflow instance of the specified workflow definition and then invokes the workflow instance.
    /// </summary>
    Task<InvokeWorkflowResult> InvokeAsync(InvokeWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invokes the specified workflow instance.
    /// </summary>
    Task<InvokeWorkflowResult> InvokeAsync(InvokeWorkflowInstanceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes the specified workflow instance.
    /// </summary>
    Task<InvokeWorkflowResult> InvokeAsync(WorkflowInstance workflowInstance, Bookmark? bookmark = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes the specified workflow.
    /// </summary>
    Task<InvokeWorkflowResult> InvokeAsync(Workflow workflow, WorkflowState workflowState, Bookmark? bookmark = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
}