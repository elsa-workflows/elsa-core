using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management;

/// <summary>
/// Extension methods for <see cref="IWorkflowDefinitionService"/>.
/// </summary>
public static class WorkflowDefinitionServiceExtensions
{
    /// <summary>
    /// Looks for a <see cref="WorkflowDefinition"/> by the specified definition ID and <see cref="VersionOptions"/>.
    /// </summary>
    public static Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(this IWorkflowDefinitionService service, string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        return service.FindWorkflowDefinitionAsync(definitionId, versionOptions, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Looks for a <see cref="WorkflowDefinition"/> by the specified version ID.
    /// </summary>
    public static Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(this IWorkflowDefinitionService service, string definitionVersionId, CancellationToken cancellationToken = default)
    {
        return service.FindWorkflowDefinitionAsync(definitionVersionId, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Looks for a <see cref="Workflow"/> by the specified definition ID and <see cref="VersionOptions"/>.
    /// </summary>
    public static async Task<Workflow?> FindWorkflowAsync(this IWorkflowDefinitionService service, string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        return (await service.FindWorkflowGraphAsync(definitionId, versionOptions, cancellationToken: cancellationToken))?.Workflow;
    }

    /// <summary>
    /// Looks for a <see cref="Workflow"/> by the specified version ID.
    /// </summary>
    public static async Task<Workflow?> FindWorkflowAsync(this IWorkflowDefinitionService service, string definitionVersionId, CancellationToken cancellationToken = default)
    {
        return (await service.FindWorkflowGraphAsync(definitionVersionId, cancellationToken: cancellationToken))?.Workflow;
    }
}