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
        var result = Read(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var result = Read(query => Filter(query, filter).OrderBy(order)).FirstOrDefault();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = Read(query => Filter(query, filter)).LongCount();
        var result = Read(query => Filter(query, filter).Paginate(pageArgs)).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    /// <inheritdoc />
    public Task<Page<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = Read(query => Filter(query, filter).OrderBy(order)).LongCount();
        var result = Read(query => Filter(query, filter).Paginate(pageArgs)).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var result = Read(query => Filter(query, filter)).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var result = Read(query => Filter(query, filter).OrderBy(order)).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = Read(query => Filter(query, filter)).LongCount();
        var result = Read(query => Filter(query, filter).Paginate(pageArgs)).Select(WorkflowDefinitionSummary.FromDefinition).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    /// <inheritdoc />
    public Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = Read(query => Filter(query, filter).OrderBy(order)).LongCount();
        var result = Read(query => Filter(query, filter).Paginate(pageArgs)).Select(WorkflowDefinitionSummary.FromDefinition).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var result = Read(query => Filter(query, filter)).Select(WorkflowDefinitionSummary.FromDefinition).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var result = Read(query => Filter(query, filter).OrderBy(order)).Select(WorkflowDefinitionSummary.FromDefinition).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken)
    {
        var result = Read(query => Filter(query, filter)).MaxBy(x => x.Version);
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
    public Task<bool> TryAddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (store.Sync)
        {
            definition.TenantId ??= CurrentTenantId;
            var key = GetVersionKey(definition);
            var added = store.Find(existing => GetVersionKey(existing) == key) == null && store.TryAdd(definition, GetId);
            return Task.FromResult(added);
        }
    }

    /// <inheritdoc />
    public Task<bool> TryRetractAsync(WorkflowDefinition expected, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (store.Sync)
        {
            var current = store.FindById(expected.Id);
            if (current == null || !current.IsPublished || current.DefinitionId != expected.DefinitionId || current.Version != expected.Version ||
                current.TenantId != expected.TenantId || current.IsReadonly != expected.IsReadonly || current.IsSystem != expected.IsSystem ||
                !CanWrite(current.TenantId))
                return Task.FromResult(false);

            var retracted = current.ShallowClone();
            retracted.IsPublished = false;
            store.Save(retracted, GetId);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<bool> TryPublishAsync(WorkflowDefinition definition, Func<IReadOnlyCollection<WorkflowDefinition>, bool> matchesExpected,
        CancellationToken cancellationToken = default) =>
        TryWriteLatestAsync(definition, matchesExpected, promoteDraft: false, cancellationToken);

    /// <inheritdoc />
    public Task<bool> TryPromoteExistingDraftAsync(WorkflowDefinition definition, Func<IReadOnlyCollection<WorkflowDefinition>, bool> matchesExpected,
        CancellationToken cancellationToken = default) =>
        TryWriteLatestAsync(definition, matchesExpected, promoteDraft: true, cancellationToken);

    private Task<bool> TryWriteLatestAsync(WorkflowDefinition definition, Func<IReadOnlyCollection<WorkflowDefinition>, bool> matchesExpected,
        bool promoteDraft, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (store.Sync)
        {
            var filter = new WorkflowDefinitionFilter { DefinitionId = definition.DefinitionId };
            var current = store.Query(query => Filter(query, filter).Where(version =>
                version.IsLatest || (promoteDraft ? version.Id == definition.Id : version.IsPublished))).ToList();
            if (current.Count == 0 || current.Any(version => !CanWrite(version.TenantId)) || !matchesExpected(current))
                return Task.FromResult(false);

            var replacing = current.SingleOrDefault(version => version.Id == definition.Id);
            if (promoteDraft && (current.Count != 2 || replacing == null || replacing.IsLatest || replacing.IsPublished || replacing.IsReadonly ||
                                 replacing.IsSystem || current.Any(version => version.TenantId != replacing.TenantId) ||
                                 !definition.IsLatest || definition.IsPublished))
            {
                return Task.FromResult(false);
            }

            if (replacing != null)
            {
                if (replacing.TenantId != definition.TenantId || replacing.Version != definition.Version)
                    return Task.FromResult(false);
            }
            else
            {
                definition.TenantId ??= CurrentTenantId;
                var versionKey = GetVersionKey(definition);
                if (!CanWrite(definition.TenantId) || store.FindById(definition.Id) != null ||
                    store.Find(version => GetVersionKey(version) == versionKey) != null)
                    return Task.FromResult(false);
            }

            foreach (var previous in current.Where(version => version.Id != definition.Id))
            {
                var retired = previous.ShallowClone();
                if (!promoteDraft)
                {
                    retired.IsPublished = false;
                }
                retired.IsLatest = false;
                store.Save(retired, GetId);
            }

            store.Save(definition.ShallowClone(), GetId);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public bool SupportsDeletion(WorkflowDefinitionDeletionPlan plan) => true;

    /// <inheritdoc />
    public Task<bool> TryDeleteAsync(WorkflowDefinitionDeletionPlan plan, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (store.Sync)
        {
            var filter = new WorkflowDefinitionFilter { DefinitionIds = plan.DefinitionIds };
            var current = store.Query(query => Filter(query, filter)).ToArray();
            if (!plan.Matches(current) || plan.Deleted.Concat(plan.Promoted).Any(version => !CanWrite(version.TenantId)))
                return Task.FromResult(false);

            store.DeleteMany(plan.Deleted.Select(version => version.Id));
            foreach (var version in plan.Promoted)
            {
                var promoted = store.FindById(version.Id)!.ShallowClone();
                promoted.IsLatest = true;
                store.Save(promoted, GetId);
            }

            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task SaveManyAsync(IEnumerable<WorkflowDefinition> definitions, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var definitionList = definitions.ToList();
            EnsureBatchVersionKeysUnique(definitionList);

            foreach (var definition in definitionList)
                EnsureVersionKeyAvailable(definition);

            store.SaveMany(definitionList, GetId);
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
                var nextVersionKey = GetVersionKey(next);
                if (store.Find(x => GetVersionKey(x) == nextVersionKey) != null || !store.TryAdd(next, GetId))
                    return Task.FromResult(WorkflowDefinitionUpdateResult.Conflict());

                current.IsLatest = false;
                store.Save(current, GetId);
            }
            else
            {
                EnsureVersionKeyAvailable(next);
                store.Save(next, GetId);
            }

            return Task.FromResult(WorkflowDefinitionUpdateResult.Updated(next));
        }
    }

    /// <inheritdoc />
    public Task<long> DeleteAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var versionIds = store.Query(query => Filter(query, filter)).Select(x => x.Id).ToList();
            return Task.FromResult(store.DeleteMany(versionIds));
        }
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var exists = Read(query => Filter(query, filter)).Any();
        return Task.FromResult(exists);
    }

    /// <inheritdoc />
    public Task<long> CountDistinctAsync(CancellationToken cancellationToken = default)
    {
        var count = Read(query => query.WhereVisibleToTenant(CurrentTenantId))
            .Select(x => x.DefinitionId)
            .Distinct()
            .LongCount();
        return Task.FromResult(count);
    }

    /// <inheritdoc />
    public Task<bool> GetIsNameUnique(string name, string? definitionId = default, CancellationToken cancellationToken = default)
    {
        var exists = Read(query => query.Where(x =>
            x.Name == name
            && x.DefinitionId != definitionId
            && TenantVisibility.IsVisible(x.TenantId, CurrentTenantId))).Any();
        return Task.FromResult(!exists);
    }

    /// <remarks>
    /// Ambient tenant is applied here rather than in <see cref="WorkflowDefinitionFilter.Apply"/>.
    /// EF owns that via <c>SetTenantIdFilter</c> / <c>IgnoreQueryFilters</c>; Memory must compensate.
    /// </remarks>
    private IQueryable<WorkflowDefinition> Filter(IQueryable<WorkflowDefinition> queryable, WorkflowDefinitionFilter filter) =>
        filter.Apply(queryable.WhereVisibleToTenant(CurrentTenantId, filter.TenantAgnostic));

    private IEnumerable<WorkflowDefinition> Read(Func<IQueryable<WorkflowDefinition>, IQueryable<WorkflowDefinition>> query)
    {
        IQueryable<WorkflowDefinition> snapshot;
        lock (store.Sync)
            snapshot = store.Queryable;
        return query(snapshot);
    }

    private string CurrentTenantId => tenantAccessor?.TenantId ?? Tenant.DefaultTenantId;

    private bool CanWrite(string? tenantId) => TenantVisibility.CanReplace(tenantId, CurrentTenantId) ||
        tenantId == Tenant.AgnosticTenantId && SharedTenantWriteScope.IsGranted(typeof(WorkflowDefinition));

    /// <remarks>
    /// Memory counterpart of the EF unique index <c>IX_WorkflowDefinition_DefinitionId_TenantId_Version</c>.
    /// Tenant is part of the key so same-tenant duplicates fail closed while cross-tenant rows stay distinct.
    /// A null tenant is normalized to "" because EF stores it that way.
    /// </remarks>
    private void EnsureVersionKeyAvailable(WorkflowDefinition definition)
    {
        var versionKey = GetVersionKey(definition);
        var existing = store.Find(x => x.Id != definition.Id && GetVersionKey(x) == versionKey);

        if (existing is not null)
            throw CreateVersionKeyConflict(definition);
    }

    private static void EnsureBatchVersionKeysUnique(IEnumerable<WorkflowDefinition> definitions)
    {
        var seen = new Dictionary<DefinitionVersionKey, string>();

        foreach (var definition in definitions)
        {
            var versionKey = GetVersionKey(definition);

            if (seen.TryGetValue(versionKey, out var existingId) && existingId != definition.Id)
                throw CreateVersionKeyConflict(definition);

            seen[versionKey] = definition.Id;
        }
    }

    private static InvalidOperationException CreateVersionKeyConflict(WorkflowDefinition definition) =>
        new($"A workflow definition already exists for definition '{definition.DefinitionId}' version {definition.Version} tenant '{definition.TenantId}'.");

    private static DefinitionVersionKey GetVersionKey(WorkflowDefinition definition) =>
        new(definition.DefinitionId, definition.Version, definition.TenantId.NormalizeTenantId());

    private string GetId(WorkflowDefinition workflowDefinition) => workflowDefinition.Id;

    private readonly record struct DefinitionVersionKey(string DefinitionId, int Version, string TenantId);
}
