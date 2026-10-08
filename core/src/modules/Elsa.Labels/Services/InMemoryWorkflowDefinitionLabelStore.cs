using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;

namespace Elsa.Labels.Services;

/// <summary>
/// An in-memory store of workflow-label associations.
/// </summary>
/// <remarks>
/// Ambient tenant is applied here rather than in callers.
/// EF owns that via <c>SetTenantIdFilter</c> / <c>ApplyTenantId</c>; Memory must compensate.
/// Labels contracts have no TenantAgnostic flag, so isolation always applies (EF query filter).
/// </remarks>
public class InMemoryWorkflowDefinitionLabelStore : IWorkflowDefinitionLabelStore, IWorkflowDefinitionLabelQuery
{
    private readonly MemoryStore<WorkflowDefinitionLabel> _store;
    private readonly ITenantAccessor? _tenantAccessor;

    /// <summary>
    /// Constructor.
    /// </summary>
    public InMemoryWorkflowDefinitionLabelStore(MemoryStore<WorkflowDefinitionLabel> store, ITenantAccessor? tenantAccessor = null)
    {
        _store = store;
        _tenantAccessor = tenantAccessor;
    }

    /// <inheritdoc />
    public Task SaveAsync(WorkflowDefinitionLabel record, CancellationToken cancellationToken = default)
    {
        ApplyCurrentTenant(record);
        lock (_store.Sync)
            _store.Save(record, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveManyAsync(IEnumerable<WorkflowDefinitionLabel> records, CancellationToken cancellationToken = default)
    {
        var list = records.ToList();

        foreach (var record in list)
            ApplyCurrentTenant(record);

        lock (_store.Sync)
            _store.SaveMany(list, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
            return Task.FromResult(_store.DeleteWhere(x => x.Id == id && IsVisible(x)) > 0);
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinitionLabel>> FindByWorkflowDefinitionVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => query.WhereVisibleToTenant(CurrentTenantId).Where(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId));
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinitionLabel>> FindByLabelIdsAsync(IEnumerable<string> labelIds, CancellationToken cancellationToken = default)
    {
        var ids = labelIds.ToHashSet();
        var result = _store.Query(query => query.WhereVisibleToTenant(CurrentTenantId).Where(x => ids.Contains(x.LabelId)));
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task ReplaceAsync(IEnumerable<WorkflowDefinitionLabel> removed, IEnumerable<WorkflowDefinitionLabel> added, CancellationToken cancellationToken = default)
    {
        var removedIds = removed.Select(x => x.Id).ToHashSet();
        var addedList = added.ToList();

        foreach (var record in addedList)
            ApplyCurrentTenant(record);

        lock (_store.Sync)
        {
            _store.DeleteWhere(x => removedIds.Contains(x.Id) && IsVisible(x));
            _store.SaveMany(addedList, x => x.Id);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<long> DeleteByWorkflowDefinitionIdAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
            return Task.FromResult(_store.DeleteWhere(x => x.WorkflowDefinitionId == workflowDefinitionId && IsVisible(x)));
    }

    /// <inheritdoc />
    public Task<long> DeleteByWorkflowDefinitionVersionIdAsync(string workflowDefinitionVersionId, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
            return Task.FromResult(_store.DeleteWhere(x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId && IsVisible(x)));
    }

    /// <inheritdoc />
    public Task<long> DeleteByWorkflowDefinitionIdsAsync(IEnumerable<string> workflowDefinitionIds, CancellationToken cancellationToken = default)
    {
        var ids = workflowDefinitionIds.ToList();
        lock (_store.Sync)
            return Task.FromResult(_store.DeleteWhere(x => ids.Contains(x.WorkflowDefinitionId) && IsVisible(x)));
    }

    /// <inheritdoc />
    public Task<long> DeleteByWorkflowDefinitionVersionIdsAsync(IEnumerable<string> workflowDefinitionVersionIds, CancellationToken cancellationToken = default)
    {
        var ids = workflowDefinitionVersionIds.ToList();
        lock (_store.Sync)
            return Task.FromResult(_store.DeleteWhere(x => ids.Contains(x.WorkflowDefinitionVersionId) && IsVisible(x)));
    }

    private bool IsVisible(Entity entity) => TenantVisibility.IsVisible(entity.TenantId, CurrentTenantId);

    private string CurrentTenantId => _tenantAccessor?.TenantId ?? Tenant.DefaultTenantId;

    private void ApplyCurrentTenant(Entity entity)
    {
        if (entity.TenantId == Tenant.AgnosticTenantId || _tenantAccessor is null)
            return;

        entity.TenantId ??= _tenantAccessor.TenantId;
    }
}
