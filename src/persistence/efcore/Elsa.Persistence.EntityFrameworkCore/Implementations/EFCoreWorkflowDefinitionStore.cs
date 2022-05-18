using System.Linq.Expressions;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Elsa.Persistence.Models;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.Services;

namespace Elsa.Persistence.EntityFrameworkCore.Implementations;

public class EFCoreWorkflowDefinitionStore : IWorkflowDefinitionStore
{
    private readonly IStore<WorkflowDefinition> _store;
    private readonly IStore<WorkflowInstance> _instanceStore;
    private readonly IStore<WorkflowTrigger> _triggerStore;
    private readonly IStore<WorkflowBookmark> _bookmarkStore;
    private readonly IStore<WorkflowExecutionLogRecord> _executionLogRecordStore;

    public EFCoreWorkflowDefinitionStore(
        IStore<WorkflowDefinition> store,
        IStore<WorkflowInstance> instanceStore,
        IStore<WorkflowTrigger> triggerStore,
        IStore<WorkflowBookmark> bookmarkStore,
        IStore<WorkflowExecutionLogRecord> executionLogRecordStore)
    {
        _store = store;
        _instanceStore = instanceStore;
        _triggerStore = triggerStore;
        _bookmarkStore = bookmarkStore;
        _executionLogRecordStore = executionLogRecordStore;
    }

    public async Task<WorkflowDefinition?> FindByIdAsync(string id, CancellationToken cancellationToken = default) => 
        await _store.FindAsync(x => x.Id == id, cancellationToken);

    public async Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        Expression<Func<WorkflowDefinition, bool>> predicate = x => x.DefinitionId == definitionId;
        predicate = predicate.WithVersion(versionOptions);
        return await _store.FindAsync(predicate, cancellationToken);
    }

    public async Task<WorkflowDefinition?> FindByNameAsync(string name, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        Expression<Func<WorkflowDefinition, bool>> predicate = x => x.Name == name;
        predicate = predicate.WithVersion(versionOptions);
        return await _store.FindAsync(predicate, cancellationToken);
    }

    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindManySummariesAsync(IEnumerable<string> definitionIds, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowDefinitions;
        var query = set.AsQueryable();

        if (versionOptions != null)
            query = query.WithVersion(versionOptions.Value);

        query = query.Where(x => definitionIds.Contains(x.DefinitionId));

        return query.OrderBy(x => x.Name).Select(x => WorkflowDefinitionSummary.FromDefinition(x)).ToList();
    }

    public async Task<IEnumerable<WorkflowDefinition>> FindLatestAndPublishedByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default) =>
        await _store.FindManyAsync(x => x.DefinitionId == definitionId && (x.IsLatest || x.IsPublished), cancellationToken);

    public async Task SaveAsync(WorkflowDefinition record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);
    public async Task SaveManyAsync(IEnumerable<WorkflowDefinition> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, cancellationToken);

    public async Task<int> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        await _triggerStore.DeleteWhereAsync(x => x.WorkflowDefinitionId == definitionId, cancellationToken);
        await _executionLogRecordStore.DeleteWhereAsync(x => x.WorkflowDefinitionId == definitionId, cancellationToken);
        await _instanceStore.DeleteWhereAsync(x => x.DefinitionId == definitionId, cancellationToken);
        await _bookmarkStore.DeleteWhereAsync(x => x.WorkflowDefinitionId == definitionId, cancellationToken);
        return await _store.DeleteWhereAsync(x => x.DefinitionId == definitionId, cancellationToken);
    }

    public async Task<int> DeleteManyByDefinitionIdsAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var definitionIdList = definitionIds.ToList();
        await _triggerStore.DeleteWhereAsync(x => definitionIdList.Contains(x.WorkflowDefinitionId), cancellationToken);
        await _executionLogRecordStore.DeleteWhereAsync(x => definitionIdList.Contains(x.WorkflowDefinitionId), cancellationToken);
        await _instanceStore.DeleteWhereAsync(x => definitionIdList.Contains(x.DefinitionId), cancellationToken);
        await _bookmarkStore.DeleteWhereAsync(x => definitionIdList.Contains(x.WorkflowDefinitionId), cancellationToken);
        return await _store.DeleteWhereAsync(x => definitionIdList.Contains(x.DefinitionId), cancellationToken);
    }

    public async Task<Page<WorkflowDefinitionSummary>> ListSummariesAsync(VersionOptions? versionOptions = default, string? materializerName = default, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowDefinitions;
        var query = set.AsQueryable();

        if (versionOptions != null)
            query = set.WithVersion(versionOptions.Value);

        if (!string.IsNullOrWhiteSpace(materializerName))
            query = set.Where(x => x.MaterializerName == materializerName);

        query = query.OrderBy(x => x.Name);

        return await Extensions.QueryableExtensions.PaginateAsync(query, x => WorkflowDefinitionSummary.FromDefinition(x), pageArgs);
    }

    public async Task<bool> GetExistsAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        Expression<Func<WorkflowDefinition, bool>> predicate = x => x.DefinitionId == definitionId;
        predicate = predicate.WithVersion(versionOptions);
        return await _store.AnyAsync(predicate, cancellationToken);
    }
}