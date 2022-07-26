using Elsa.Persistence.Common.Extensions;
using Elsa.Persistence.Common.Implementations;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Extensions;
using Elsa.Workflows.Persistence.Models;
using Elsa.Workflows.Persistence.Services;

namespace Elsa.Workflows.Persistence.Implementations;

public class MemoryWorkflowDefinitionStore : IWorkflowDefinitionStore
{
    private readonly MemoryStore<WorkflowDefinition> _store;
    private readonly MemoryStore<WorkflowInstance> _instanceStore;
    private readonly MemoryStore<WorkflowTrigger> _triggerStore;
    private readonly MemoryStore<WorkflowBookmark> _bookmarkStore;

    public MemoryWorkflowDefinitionStore(
        MemoryStore<WorkflowDefinition> store,
        MemoryStore<WorkflowInstance> instanceStore,
        MemoryStore<WorkflowTrigger> triggerStore,
        MemoryStore<WorkflowBookmark> bookmarkStore)
    {
        _store = store;
        _instanceStore = instanceStore;
        _triggerStore = triggerStore;
        _bookmarkStore = bookmarkStore;
    }

    public Task<WorkflowDefinition?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var definition = _store.Find(x => x.Id == id);
        return Task.FromResult(definition);
    }

    public Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var definition = _store.Find(x => x.DefinitionId == definitionId && x.WithVersion(versionOptions));
        return Task.FromResult(definition);
    }

    public Task<WorkflowDefinition?> FindByNameAsync(string name, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var definition = _store.Find(x => x.Name == name && x.WithVersion(versionOptions));
        return Task.FromResult(definition);
    }

    public Task<IEnumerable<WorkflowDefinitionSummary>> FindManySummariesAsync(IEnumerable<string> definitionIds, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default)
    {
        var query = _store.List();

        if (versionOptions != null)
            query = query.WithVersion(versionOptions.Value);

        var definitionIdList = definitionIds.ToList();
        query = query.Where(x => definitionIdList.Contains(x.Id));

        var summaries = query.Select(WorkflowDefinitionSummary.FromDefinition).ToList().AsEnumerable();
        return Task.FromResult(summaries);
    }

    public Task<IEnumerable<WorkflowDefinition>> FindLatestAndPublishedByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var definitions = _store.FindMany(x => x.DefinitionId == definitionId && (x.IsLatest || x.IsPublished));
        return Task.FromResult(definitions);
    }

    public Task SaveAsync(WorkflowDefinition record, CancellationToken cancellationToken = default)
    {
        _store.Save(record);
        return Task.CompletedTask;
    }

    public Task SaveManyAsync(IEnumerable<WorkflowDefinition> records, CancellationToken cancellationToken = default)
    {
        _store.SaveMany(records);
        return Task.CompletedTask;
    }

    public Task<int> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        _triggerStore.DeleteWhere(x => x.WorkflowDefinitionId == definitionId);
        _instanceStore.DeleteWhere(x => x.DefinitionId == definitionId);
        _bookmarkStore.DeleteWhere(x => x.WorkflowDefinitionId == definitionId);
        var result = _store.DeleteWhere(x => x.DefinitionId == definitionId);
        return Task.FromResult(result);
    }

    public Task<int> DeleteByDefinitionIdsAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var definitionIdList = definitionIds.ToList();
        _triggerStore.DeleteWhere(x => definitionIdList.Contains(x.WorkflowDefinitionId));
        _instanceStore.DeleteWhere(x => definitionIdList.Contains(x.DefinitionId));
        _bookmarkStore.DeleteWhere(x => definitionIdList.Contains(x.WorkflowDefinitionId));
        var result = _store.DeleteWhere(x => definitionIdList.Contains(x.DefinitionId));
        return Task.FromResult(result);
    }

    public Task<Page<WorkflowDefinitionSummary>> ListSummariesAsync(
        VersionOptions? versionOptions = default,
        string? materializerName = default,
        PageArgs? pageArgs = default,
        CancellationToken cancellationToken = default)
    {
        var query = _store.List().AsQueryable();

        if (versionOptions != null) query = query.WithVersion(versionOptions.Value);
        if (!string.IsNullOrWhiteSpace(materializerName)) query = query.Where(x => x.MaterializerName == materializerName);

        var page = query.Paginate(x => WorkflowDefinitionSummary.FromDefinition(x), pageArgs);
        return Task.FromResult(page);
    }

    public Task<bool> GetExistsAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var exists = _store.AnyAsync(x => x.DefinitionId == definitionId && x.WithVersion(versionOptions));
        return Task.FromResult(exists);
    }

    private IQueryable<WorkflowDefinition> FilterByLabels(IQueryable<WorkflowDefinition> query, IEnumerable<string>? labelNames)
    {
        // var labelList = labelNames?.Select(x => x.ToLowerInvariant()).ToList();
        //
        // // Do we need to filter by labels?
        // if (labelList == null || !labelList.Any())
        //     return query;
        //
        // // Translate label names to label IDs.
        // var labelIds = _labelStore.FindMany(x => labelList.Contains(x.NormalizedName)).Select(x => x.Id);
        //
        // // We need to build a query that requires a workflow definition to be associated with ALL labels ("and").
        // foreach (var labelId in labelIds)
        //     query =
        //         from workflowDefinition in query
        //         join label in _workflowDefinitionLabelStore.List().AsQueryable()
        //             on workflowDefinition.Id equals label.WorkflowDefinitionVersionId
        //         where labelId == label.LabelId
        //         select workflowDefinition;

        return query;
    }
}