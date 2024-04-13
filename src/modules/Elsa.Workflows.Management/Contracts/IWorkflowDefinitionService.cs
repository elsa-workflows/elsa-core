using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;

namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// Manages materialization of <see cref="WorkflowDefinition"/> to <see cref="Workflow"/> objects. 
/// </summary>
public interface IWorkflowDefinitionService
{
    /// <summary>
    /// Constructs an executable <see cref="Workflow"/> from the specified <see cref="WorkflowDefinition"/>.
    /// </summary>
    Task<Workflow> MaterializeWorkflowAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Looks for a <see cref="WorkflowDefinition"/> by the specified definition ID and <see cref="VersionOptions"/>.
    /// </summary>
    Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Looks for a <see cref="WorkflowDefinition"/> by the specified version ID.
    /// </summary>
    Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionVersionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Looks for a <see cref="WorkflowDefinition"/> by the specified <see cref="WorkflowDefinitionFilter"/>.
    /// </summary>
    Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Looks for a <see cref="Workflow"/> by the specified definition ID and <see cref="VersionOptions"/>.
    /// </summary>
    Task<Workflow?> FindWorkflowAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Looks for a <see cref="Workflow"/> by the specified version ID.
    /// </summary>
    Task<Workflow?> FindWorkflowAsync(string definitionVersionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Looks for a <see cref="Workflow"/> by the specified <see cref="WorkflowDefinitionFilter"/>.
    /// </summary>
    Task<Workflow?> FindWorkflowAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);
}