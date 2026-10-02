using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Contracts;

/// <summary>
/// Provides functionality for cloning a workflow definitions.
/// </summary>
public interface IWorkflowCloningDialogService
{
    /// <summary>
    /// Creates a duplicate of the specified workflow definition.
    /// </summary>
    /// <param name="workflowDefinition">The workflow definition to duplicate. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="Result{TSuccess,
    /// TFailure}"/> object that holds either a <see cref="SaveWorkflowDefinitionResponse"/> on success or <see
    /// cref="ValidationErrors"/> on failure. Returns <see langword="null"/> if the operation cannot be completed.</returns>
    Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>?> Duplicate(WorkflowDefinition workflowDefinition);
    

    /// <summary>
    /// Creates a save as the specified workflow definition as a new entity, creating a copy with a unique identifier.
    /// </summary>
    /// <param name="workflowDefinition">The workflow definition to save as a new entity. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="Result{TSuccess,
    /// TFailure}"/> object that holds either a <see cref="SaveWorkflowDefinitionResponse"/> on success or <see
    /// cref="ValidationErrors"/> on failure. Returns <see langword="null"/> if the operation is not completed.</returns>
    Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>?> SaveAs(WorkflowDefinition workflowDefinition);
}