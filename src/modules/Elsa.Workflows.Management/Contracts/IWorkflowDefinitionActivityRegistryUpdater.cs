namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// Represents a service for updating the activity registry.
/// </summary>
public interface IWorkflowDefinitionActivityRegistryUpdater
{
    /// <summary>
    /// Tries to add a workflow as an activity to the registry.
    /// </summary>
    /// <param name="workflowDefinitionVersionId">The version ID of the workflow definition.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddToRegistry(string workflowDefinitionVersionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes workflow definition activities from the <see cref="IActivityRegistry"/>.
    /// </summary>
    /// <param name="workflowDefinitionId">The ID of the workflow definition to remove.</param>
    void RemoveDefinitionFromRegistry(string workflowDefinitionId);
    
    /// <summary>
    /// Removes a workflow definition version activity from the <see cref="IActivityRegistry"/>.
    /// </summary>
    /// <param name="workflowDefinitionVersionId">The ID of the workflow definition to remove.</param>
    void RemoveDefinitionVersionFromRegistry(string workflowDefinitionVersionId);
}