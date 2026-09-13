using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Stores;

/// <summary>
/// A memory implementation of <see cref="IWorkflowDefinitionStore"/>.
/// </summary>
public class MemoryWorkflowDefinitionStore(MemoryStore<WorkflowDefinition> store, ITenantAccessor? tenantAccessor = null) : IWorkflowDefinitionStore
{
    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var result = store.Query(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var result = store.Query(query => Filter(query, filter).OrderBy(order)).FirstOrDefault();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = store.Query(query => Filter(query, filter)).LongCount();
        var result = store.Query(query => Filter(query, filter).Paginate(pageArgs)).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    /// <inheritdoc />
    public Task<Page<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = store.Query(query => Filter(query, filter).OrderBy(order)).LongCount();
        var result = store.Query(query => Filter(query, filter).Paginate(pageArgs)).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var result = store.Query(query => Filter(query, filter)).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var result = store.Query(query => Filter(query, filter).OrderBy(order)).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = store.Query(query => Filter(query, filter)).LongCount();
        var result = store.Query(query => Filter(query, filter).Paginate(pageArgs)).Select(WorkflowDefinitionSummary.FromDefinition).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    /// <inheritdoc />
    public Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = store.Query(query => Filter(query, filter).OrderBy(order)).LongCount();
        var result = store.Query(query => Filter(query, filter).Paginate(pageArgs)).Select(WorkflowDefinitionSummary.FromDefinition).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var result = store.Query(query => Filter(query, filter)).Select(WorkflowDefinitionSummary.FromDefinition).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var result = store.Query(query => Filter(query, filter).OrderBy(order)).Select(WorkflowDefinitionSummary.FromDefinition).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken)
    {
        var result = store.Query(query => Filter(query, filter)).MaxBy(x => x.Version);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            EnsureVersionKeyAvailable(definition);
            store.Save(definition, GetId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveManyAsync(IEnumerable<WorkflowDefinition> definitions, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var uniqueDefinitions = DistinctByVersionKey(definitions).ToList();

            foreach (var definition in uniqueDefinitions)
                EnsureVersionKeyAvailable(definition);

            store.SaveMany(uniqueDefinitions, GetId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<WorkflowDefinitionUpdateResult> TryUpdateLatestAsync(
        WorkflowDefinitionFilter filter,
        Func<WorkflowDefinition, bool> matchesExpected,
        Func<WorkflowDefinition, WorkflowDefinition> update,
        CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var current = store.Query(query => Filter(query, filter)).FirstOrDefault();

            if (current == null)
                return Task.FromResult(WorkflowDefinitionUpdateResult.NotFound());

            if (!current.IsLatest || !matchesExpected(current))
                return Task.FromResult(WorkflowDefinitionUpdateResult.Conflict());

            var next = update(current);

            if (next.Id != current.Id)
            {
                current.IsLatest = false;
                store.Save(current, GetId);
            }

            store.Save(next, GetId);
            return Task.FromResult(WorkflowDefinitionUpdateResult.Updated(next));
        }
    }

    /// <inheritdoc />
    public Task<long> DeleteAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var workflowDefinitionIds = store.Query(query => Filter(query, filter)).Select(x => x.DefinitionId).Distinct().ToList();
            store.DeleteWhere(x =>
                workflowDefinitionIds.Contains(x.DefinitionId)
                && (filter.TenantAgnostic || TenantVisibility.IsVisible(x.TenantId, CurrentTenantId)));
            return Task.FromResult(workflowDefinitionIds.LongCount());
        }
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var exists = store.Query(query => Filter(query, filter)).Any();
        return Task.FromResult(exists);
    }

    /// <inheritdoc />
    public Task<long> CountDistinctAsync(CancellationToken cancellationToken = default)
    {
        var count = store.Query(query => query.WhereVisibleToTenant(CurrentTenantId))
            .Select(x => x.DefinitionId)
            .Distinct()
            .LongCount();
        return Task.FromResult(count);
    }

    /// <inheritdoc />
    public Task<bool> GetIsNameUnique(string name, string? definitionId = default, CancellationToken cancellationToken = default)
    {
        var exists = store.Any(x =>
            x.Name == name
            && x.DefinitionId != definitionId
            && TenantVisibility.IsVisible(x.TenantId, CurrentTenantId));
        return Task.FromResult(!exists);
    }

    /// <remarks>
    /// Ambient tenant is applied here rather than in <see cref="WorkflowDefinitionFilter.Apply"/>.
    /// EF owns that via <c>SetTenantIdFilter</c> / <c>IgnoreQueryFilters</c>; Memory must compensate.
    /// </remarks>
    private IQueryable<WorkflowDefinition> Filter(IQueryable<WorkflowDefinition> queryable, WorkflowDefinitionFilter filter) =>
        filter.Apply(queryable.WhereVisibleToTenant(CurrentTenantId, filter.TenantAgnostic));

    private string CurrentTenantId => tenantAccessor?.TenantId ?? Tenant.DefaultTenantId;

    /// <remarks>
    /// EF enforces <c>(DefinitionId, Version)</c> globally via
    /// <c>IX_WorkflowDefinition_DefinitionId_Version</c>. Memory keeps tenant in the key so
    /// same-tenant duplicates fail closed while cross-tenant rows remain distinct until #7539
    /// adds <c>TenantId</c> to that index.
    /// </remarks>
    private void EnsureVersionKeyAvailable(WorkflowDefinition definition)
    {
        var versionKey = GetVersionKey(definition);
        var existing = store.Find(x => x.Id != definition.Id && GetVersionKey(x) == versionKey);

        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"A workflow definition already exists for definition '{definition.DefinitionId}' version {definition.Version} tenant '{definition.TenantId}'.");
        }
    }

    private static IEnumerable<WorkflowDefinition> DistinctByVersionKey(IEnumerable<WorkflowDefinition> definitions)
    {
        var seen = new HashSet<DefinitionVersionKey>();

        foreach (var definition in definitions)
        {
            if (seen.Add(GetVersionKey(definition)))
                yield return definition;
        }
    }

    private static DefinitionVersionKey GetVersionKey(WorkflowDefinition definition) =>
        new(definition.DefinitionId, definition.Version, definition.TenantId);

    private string GetId(WorkflowDefinition workflowDefinition) => workflowDefinition.Id;

    private readonly record struct DefinitionVersionKey(string DefinitionId, int Version, string? TenantId);
}