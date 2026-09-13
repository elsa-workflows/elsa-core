using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;

namespace Elsa.Labels.Services;

/// <summary>
/// An in-memory store of labels.
/// </summary>
/// <remarks>
/// Ambient tenant is applied here rather than in callers.
/// EF owns that via <c>SetTenantIdFilter</c> / <c>ApplyTenantId</c>; Memory must compensate.
/// Labels contracts have no TenantAgnostic flag, so isolation always applies (EF query filter).
/// </remarks>
public class InMemoryLabelStore : ILabelStore
{
    private readonly MemoryStore<Label> _labelStore;
    private readonly MemoryStore<WorkflowDefinitionLabel> _workflowDefinitionLabelStore;
    private readonly ITenantAccessor? _tenantAccessor;

    /// <summary>
    /// Constructor.
    /// </summary>
    public InMemoryLabelStore(
        MemoryStore<Label> labelStore,
        MemoryStore<WorkflowDefinitionLabel> workflowDefinitionLabelStore,
        ITenantAccessor? tenantAccessor = null)
    {
        _labelStore = labelStore;
        _workflowDefinitionLabelStore = workflowDefinitionLabelStore;
        _tenantAccessor = tenantAccessor;
    }

    /// <inheritdoc />
    public Task SaveAsync(Label record, CancellationToken cancellationToken = default)
    {
        ApplyCurrentTenant(record);
        _labelStore.Save(record, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveManyAsync(IEnumerable<Label> records, CancellationToken cancellationToken = default)
    {
        var list = records.ToList();

        foreach (var record in list)
            ApplyCurrentTenant(record);

        _labelStore.SaveMany(list, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var label = FindVisibleLabel(id);

        if (label is null)
            return Task.FromResult(false);

        _workflowDefinitionLabelStore.DeleteWhere(x => x.LabelId == id && IsVisible(x));
        return Task.FromResult(_labelStore.Delete(id));
    }

    /// <inheritdoc />
    public Task<long> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        var visibleIds = _labelStore
            .Query(query => query.WhereVisibleToTenant(CurrentTenantId).Where(x => idList.Contains(x.Id)))
            .Select(x => x.Id)
            .ToList();

        _workflowDefinitionLabelStore.DeleteWhere(x => visibleIds.Contains(x.LabelId) && IsVisible(x));
        return Task.FromResult(_labelStore.DeleteMany(visibleIds));
    }

    /// <inheritdoc />
    public Task<Label?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(FindVisibleLabel(id));
    }

    /// <inheritdoc />
    public Task<Page<Label>> ListAsync(PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        var query = _labelStore.List().AsQueryable().WhereVisibleToTenant(CurrentTenantId).OrderBy(x => x.Name);
        var page = query.ToPage(pageArgs);
        return Task.FromResult(page);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Label>> FindManyByIdAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
    {
        var idList = ids.ToList();
        var records = _labelStore.Query(query => query.WhereVisibleToTenant(CurrentTenantId).Where(x => idList.Contains(x.Id)));
        return Task.FromResult(records);
    }

    private Label? FindVisibleLabel(string id) =>
        _labelStore.Query(query => query.WhereVisibleToTenant(CurrentTenantId).Where(x => x.Id == id)).FirstOrDefault();

    private bool IsVisible(Entity entity) => TenantVisibility.IsVisible(entity.TenantId, CurrentTenantId);

    private string CurrentTenantId => _tenantAccessor?.TenantId ?? Tenant.DefaultTenantId;

    private void ApplyCurrentTenant(Entity entity)
    {
        if (entity.TenantId == Tenant.AgnosticTenantId || _tenantAccessor is null)
            return;

        entity.TenantId ??= _tenantAccessor.TenantId;
    }
}
