using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management;

/// <summary>
/// Manages materialization of <see cref="WorkflowDefinition"/> to <see cref="Workflow"/> objects. 
/// </summary>
public interface IWorkflowDefinitionService
{
    /// <summary>
    /// Constructs an executable <see cref="Workflow"/> from the specified <see cref="WorkflowDefinition"/>.
    /// </summary>
    Task<WorkflowGraph> MaterializeWorkflowAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks for a <see cref="WorkflowDefinition"/> by the specified definition ID and <see cref="VersionOptions"/>.
    /// </summary>
    Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks for a <see cref="WorkflowDefinition"/> by the specified version ID.
    /// </summary>
    Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionVersionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks for a <see cref="WorkflowDefinition"/> by the specified <see cref="WorkflowDefinitionHandle"/>.
    /// </summary>
    Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(WorkflowDefinitionHandle handle, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Looks for a <see cref="WorkflowDefinition"/> by the specified <see cref="WorkflowDefinitionFilter"/>.
    /// </summary>
    Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks for a <see cref="WorkflowGraph"/> by the specified definition ID and <see cref="VersionOptions"/>.
    /// </summary>
    Task<WorkflowGraph?> FindWorkflowGraphAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks for a <see cref="WorkflowGraph"/> by the specified version ID.
    /// </summary>
    Task<WorkflowGraph?> FindWorkflowGraphAsync(string definitionVersionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks for a <see cref="WorkflowGraph"/> by the specified <see cref="WorkflowDefinitionHandle"/>.
    /// </summary>
    Task<WorkflowGraph?> FindWorkflowGraphAsync(WorkflowDefinitionHandle definitionHandle, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Looks for a <see cref="WorkflowGraph"/> by the specified <see cref="WorkflowDefinitionFilter"/>.
    /// </summary>
    Task<WorkflowGraph?> FindWorkflowGraphAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Looks for all <see cref="WorkflowGraph"/>s that match the specified <see cref="WorkflowDefinitionFilter"/>.
    /// </summary>
    Task<IEnumerable<WorkflowGraph>> FindWorkflowGraphsAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to retrieve a <see cref="WorkflowGraph"/> and its corresponding <see cref="WorkflowDefinition"/>
    /// based on the specified workflow definition ID and version options.
    /// </summary>
    /// <param name="definitionId">The ID of the workflow definition to search for.</param>
    /// <param name="versionOptions">The versioning options that determine which version of the workflow definition to search for.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="WorkflowGraphFindResult"/> containing the workflow graph and its associated definition,
    /// or indicating that they do not exist.
    /// </returns>
    Task<WorkflowGraphFindResult> TryFindWorkflowGraphAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to locate a <see cref="WorkflowGraph"/> based on the specified workflow definition version ID.
    /// </summary>
    /// <param name="definitionVersionId">The unique identifier of the workflow definition version.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="WorkflowGraphFindResult"/> containing the workflow graph and its associated definition,
    /// or indicating that they do not exist.
    /// </returns>
    Task<WorkflowGraphFindResult> TryFindWorkflowGraphAsync(string definitionVersionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to retrieve the workflow graph corresponding to the specified <see cref="WorkflowDefinitionHandle"/>.
    /// </summary>
    /// <param name="definitionHandle">The handle identifying the workflow definition and its version.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="WorkflowGraphFindResult"/> containing the workflow graph and its associated definition,
    /// or indicating that they do not exist.
    /// </returns>
    Task<WorkflowGraphFindResult> TryFindWorkflowGraphAsync(WorkflowDefinitionHandle definitionHandle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to retrieve a <see cref="WorkflowGraph"/> and its associated <see cref="WorkflowDefinition"/>
    /// for the specified workflow definition using the provided filter criteria.
    /// </summary>
    /// <param name="filter">The criteria used to filter and locate the workflow definition and graph.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="WorkflowGraphFindResult"/> containing the workflow graph and its associated definition,
    /// or indicating that they do not exist.
    /// </returns>
    Task<WorkflowGraphFindResult> TryFindWorkflowGraphAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to find and retrieve a collection of <see cref="WorkflowGraphFindResult"/> objects based on the specified <see cref="WorkflowDefinitionFilter"/>.
    /// </summary>
    /// <param name="filter">The filter specifying criteria to identify the workflow definitions to search for.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="WorkflowGraphFindResult"/> containing the workflow graph and its associated definition,
    /// or indicating that they do not exist.
    /// </returns>
    Task<IEnumerable<WorkflowGraphFindResult>> TryFindWorkflowGraphsAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);
}