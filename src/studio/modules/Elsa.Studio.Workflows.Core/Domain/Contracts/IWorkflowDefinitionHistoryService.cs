using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// A service that can be used to manage the history of workflow definitions.
/// </summary>
public interface IWorkflowDefinitionHistoryService
{
    /// <summary>
    /// Retracts a workflow definition.
    /// </summary>
    Task<Result<WorkflowDefinition, ValidationErrors>> RetractAsync(WorkflowDefinition workflowDefinition, Func<WorkflowDefinition, Task>? workflowRetractedCallback = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reverts the specified workflow definition to the specified version.
    /// </summary>
    Task<WorkflowDefinitionSummary> RevertAsync(WorkflowDefinitionVersion workflowDefinitionVersion, CancellationToken cancellationToken = default);
}