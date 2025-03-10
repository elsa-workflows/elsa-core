using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
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
}