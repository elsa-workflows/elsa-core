using Elsa.Persistence.Entities;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.Models;
using Elsa.Persistence.Services;

namespace Elsa.Persistence.Implementations;

public class NullWorkflowDefinitionStore : IWorkflowDefinitionStore
{
    public Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default) =>
        Task.FromResult<WorkflowDefinition?>(default);

    public Task<WorkflowDefinition?> FindByNameAsync(string name, VersionOptions versionOptions, CancellationToken cancellationToken = default) =>
        Task.FromResult<WorkflowDefinition?>(default);

    public Task<IEnumerable<WorkflowDefinitionSummary>> FindManySummariesAsync(IEnumerable<string> definitionIds, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default) =>
        Task.FromResult(Enumerable.Empty<WorkflowDefinitionSummary>());

    public Task<IEnumerable<WorkflowDefinition>> FindLatestAndPublishedByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Enumerable.Empty<WorkflowDefinition>());

    public Task SaveAsync(WorkflowDefinition record, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SaveManyAsync(IEnumerable<WorkflowDefinition> records, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<int> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default) => Task.FromResult(0);

    public Task<int> DeleteManyByDefinitionIdsAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default) => Task.FromResult(0);

    public Task<Page<WorkflowDefinitionSummary>> ListSummariesAsync(VersionOptions? versionOptions = default, string? materializerName = default, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        var page = new Page<WorkflowDefinitionSummary>(new List<WorkflowDefinitionSummary>(0), 0);
        return Task.FromResult(page);
    }
}