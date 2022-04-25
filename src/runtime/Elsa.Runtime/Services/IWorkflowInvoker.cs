using Elsa.Models;
using Elsa.Runtime.Models;

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
}