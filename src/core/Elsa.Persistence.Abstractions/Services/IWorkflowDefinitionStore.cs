using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;

namespace Elsa.Persistence.Services;

public interface IWorkflowDefinitionStore
{
    Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);
    Task<WorkflowDefinition?> FindByNameAsync(string name, VersionOptions versionOptions, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkflowDefinitionSummary>> FindManySummariesAsync(IEnumerable<string> definitionIds, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkflowDefinition>> FindLatestAndPublishedByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default);
    Task SaveAsync(WorkflowDefinition record, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<WorkflowDefinition> records, CancellationToken cancellationToken = default);
    Task<int> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default);
    Task<int> DeleteManyByDefinitionIdsAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default);
    Task<Page<WorkflowDefinitionSummary>> ListSummariesAsync(VersionOptions? versionOptions = default, string? materializerName = default, PageArgs? pageArgs = default, CancellationToken cancellationToken = default);
    Task<bool> GetExistsAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);
}