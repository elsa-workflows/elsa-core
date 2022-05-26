using Elsa.Labels.Entities;
using Elsa.Labels.Services;
using Elsa.Persistence.Common.Extensions;
using Elsa.Persistence.Common.Implementations;
using Elsa.Persistence.Common.Models;

namespace Elsa.Labels.Implementations;

public class InMemoryLabelStore : ILabelStore
{
    private readonly MemoryStore<Label> _labelStore;
    private readonly MemoryStore<WorkflowDefinitionLabel> _workflowDefinitionLabelStore;

    public InMemoryLabelStore(MemoryStore<Label> labelStore, MemoryStore<WorkflowDefinitionLabel> workflowDefinitionLabelStore)
    {
        _labelStore = labelStore;
        _workflowDefinitionLabelStore = workflowDefinitionLabelStore;
    }

    public Task SaveAsync(Label record, CancellationToken cancellationToken = default)
    {
        _labelStore.Save(record);
        return Task.CompletedTask;
    }

    public Task SaveManyAsync(IEnumerable<Label> records, CancellationToken cancellationToken = default)
    {
        _labelStore.SaveMany(records);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _workflowDefinitionLabelStore.DeleteWhere(x => x.LabelId == id);
        var result = _labelStore.Delete(id);
        return Task.FromResult(result);
    }

    public Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        _workflowDefinitionLabelStore.DeleteWhere(x => idList.Contains(x.LabelId));
        var result = _labelStore.DeleteMany(idList);
        return Task.FromResult(result);
    }

    public Task<Label?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var record = _labelStore.Find(x => x.Id == id);
        return Task.FromResult(record);
    }

    public Task<Page<Label>> ListAsync(PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        var query = _labelStore.List().AsQueryable().OrderBy(x => x.Name);
        var page = query.Paginate(pageArgs);
        return Task.FromResult(page);
    }

    public Task<IEnumerable<Label>> FindManyByIdAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
    {
        var idList = ids.ToList();
        return Task.FromResult(_labelStore.FindMany(x => idList.Contains(x.Id)));
    }
}