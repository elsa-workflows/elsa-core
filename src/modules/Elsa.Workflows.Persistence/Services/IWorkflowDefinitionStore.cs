using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Models;

namespace Elsa.Workflows.Persistence.Services;

public interface IWorkflowDefinitionStore
{
    /// <summary>
    /// Finds a workflow definition by its unique version ID.
    /// </summary>
    Task<WorkflowDefinition?> FindByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a workflow definition by its logical definition ID and specified version options.
    /// </summary>
    Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a workflow definition by name and specified version options.
    /// </summary>
    Task<WorkflowDefinition?> FindByNameAsync(string name, VersionOptions versionOptions, CancellationToken cancellationToken = default);

    Task<IEnumerable<WorkflowDefinitionSummary>> FindManySummariesAsync(IEnumerable<string> definitionIds, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkflowDefinition>> FindLatestAndPublishedByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default);
    Task SaveAsync(WorkflowDefinition record, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<WorkflowDefinition> records, CancellationToken cancellationToken = default);
    Task<int> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default);
    Task<int> DeleteByDefinitionIdsAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default);

    Task<Page<WorkflowDefinitionSummary>> ListSummariesAsync(
        VersionOptions? versionOptions = default,
        string? materializerName = default,
        PageArgs? pageArgs = default,
        CancellationToken cancellationToken = default);

    Task<bool> GetExistsAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);
}