using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;

namespace Elsa.Labels.Services;

/// <summary>
/// An in-memory store of labels.
/// </summary>
public class InMemoryLabelStore : ILabelStore
{
    private readonly MemoryStore<Label> _labelStore;
    private readonly MemoryStore<WorkflowDefinitionLabel> _workflowDefinitionLabelStore;

    /// <summary>
    /// Constructor.
    /// </summary>
    public InMemoryLabelStore(MemoryStore<Label> labelStore, MemoryStore<WorkflowDefinitionLabel> workflowDefinitionLabelStore)
    {
        _labelStore = labelStore;
        _workflowDefinitionLabelStore = workflowDefinitionLabelStore;
    }

    /// <inheritdoc />
    public Task SaveAsync(Label record, CancellationToken cancellationToken = default)
    {
        _labelStore.Save(record, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveManyAsync(IEnumerable<Label> records, CancellationToken cancellationToken = default)
    {
        _labelStore.SaveMany(records, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _workflowDefinitionLabelStore.DeleteWhere(x => x.LabelId == id);
        var result = _labelStore.Delete(id);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<long> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        _workflowDefinitionLabelStore.DeleteWhere(x => idList.Contains(x.LabelId));
        var result = _labelStore.DeleteMany(idList);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<Label?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var record = _labelStore.Find(x => x.Id == id);
        return Task.FromResult(record);
    }

    /// <inheritdoc />
    public Task<Page<Label>> ListAsync(PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        var query = _labelStore.List().AsQueryable().OrderBy(x => x.Name);
        var page = query.ToPage(pageArgs);
        return Task.FromResult(page);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Label>> FindManyByIdAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
    {
        var idList = ids.ToList();
        return Task.FromResult(_labelStore.FindMany(x => idList.Contains(x.Id)));
    }
}