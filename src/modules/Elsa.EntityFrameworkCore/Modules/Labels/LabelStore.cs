using Elsa.Common.Models;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Labels.Entities;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.Labels.Contracts;

namespace Elsa.EntityFrameworkCore.Modules.Labels;

/// <summary>
/// An Entity Framework Core implementation of <see cref="ILabelStore"/>.
/// </summary>
public class EFCoreLabelStore : ILabelStore
{
    private readonly EntityStore<LabelsElsaDbContext, Label> _labelStore;
    private readonly EntityStore<LabelsElsaDbContext, WorkflowDefinitionLabel> _workflowDefinitionLabelStore;

    /// <summary>
    /// Initializes a new instance of <see cref="EFCoreLabelStore"/>.
    /// </summary>
    public EFCoreLabelStore(EntityStore<LabelsElsaDbContext, Label> labelStore, EntityStore<LabelsElsaDbContext, WorkflowDefinitionLabel> workflowDefinitionLabelStore)
    {
        _labelStore = labelStore;
        _workflowDefinitionLabelStore = workflowDefinitionLabelStore;
    }

    /// <inheritdoc />
    public async Task SaveAsync(Label record, CancellationToken cancellationToken = default) => await _labelStore.SaveAsync(record, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<Label> records, CancellationToken cancellationToken = default) => await _labelStore.SaveManyAsync(records, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await _workflowDefinitionLabelStore.DeleteWhereAsync(x => x.LabelId == id, cancellationToken);
        return await _labelStore.DeleteWhereAsync(x => x.Id == id, cancellationToken) > 0;
    }

    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
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