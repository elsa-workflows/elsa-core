using Elsa.Common.Models;
using Elsa.Labels.Entities;
using Elsa.Labels.Services;
using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Persistence.EntityFrameworkCore.Extensions;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.Labels;

public class EFCoreLabelStore : ILabelStore
{
    private readonly Store<LabelsDbContext, Label> _labelStore;
    private readonly Store<LabelsDbContext, WorkflowDefinitionLabel> _workflowDefinitionLabelStore;

    public EFCoreLabelStore(Store<LabelsDbContext, Label> labelStore, Store<LabelsDbContext, WorkflowDefinitionLabel> workflowDefinitionLabelStore)
    {
        _labelStore = labelStore;
        _workflowDefinitionLabelStore = workflowDefinitionLabelStore;
    }

    public async Task SaveAsync(Label record, CancellationToken cancellationToken = default) => await _labelStore.SaveAsync(record, cancellationToken);
    public async Task SaveManyAsync(IEnumerable<Label> records, CancellationToken cancellationToken = default) => await _labelStore.SaveManyAsync(records, cancellationToken);

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await _workflowDefinitionLabelStore.DeleteWhereAsync(x => x.LabelId == id, cancellationToken);
        return await _labelStore.DeleteWhereAsync(x => x.Id == id, cancellationToken) > 0;
    }

    public async Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        await _workflowDefinitionLabelStore.DeleteWhereAsync(x => idList.Contains(x.LabelId), cancellationToken);
        return await _labelStore.DeleteWhereAsync(x => idList.Contains(x.Id), cancellationToken);
    }

    public async Task<Label?> FindByIdAsync(string id, CancellationToken cancellationToken = default) => await _labelStore.FindAsync(x => x.Id == id, cancellationToken);

    public async Task<Page<Label>> ListAsync(PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _labelStore.CreateDbContextAsync(cancellationToken);
        var set = dbContext.Labels.OrderBy(x => x.Name);
        return await set.PaginateAsync(pageArgs);
    }

    public async Task<IEnumerable<Label>> FindManyByIdAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await _labelStore.FindManyAsync(x => idList.Contains(x.Id), cancellationToken);
    }
}