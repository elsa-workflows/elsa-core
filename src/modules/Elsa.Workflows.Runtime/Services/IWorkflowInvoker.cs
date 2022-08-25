using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Invokes a workflow using a configured runtime (e.g. ProtoActor).
/// </summary>
public interface IWorkflowInvoker
{
    /// <summary>
    /// Creates a workflow instance of the specified workflow definition and then invokes the workflow instance.
    /// </summary>
    Task<RunWorkflowResult> InvokeAsync(InvokeWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invokes the specified workflow instance.
    /// </summary>
    Task<RunWorkflowResult> InvokeAsync(InvokeWorkflowInstanceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes the specified workflow instance.
    /// </summary>
    Task<RunWorkflowResult> InvokeAsync(WorkflowInstance workflowInstance, Bookmark? bookmark = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes the specified workflow definition.
    /// </summary>
    Task<RunWorkflowResult> InvokeAsync(WorkflowDefinition workflowDefinition, WorkflowState workflowState, Bookmark? bookmark = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invokes the specified workflow.
    /// </summary>
    Task<RunWorkflowResult> InvokeAsync(Workflow workflow, WorkflowState workflowState, Bookmark? bookmark = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
}