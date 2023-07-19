using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;
using Elsa.MongoDb.Common;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDb.Modules.Labels;

/// <summary>
/// A MongoDB based store for <see cref="Label"/>s.
/// </summary>
public class MongoLabelStore : ILabelStore
{
    private readonly MongoDbStore<Label> _labelMongoDbStore;
    private readonly MongoDbStore<WorkflowDefinitionLabel> _workflowDefinitionLabelMongoDbStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoLabelStore"/> class.
    /// </summary>
    public MongoLabelStore(MongoDbStore<Label> labelMongoDbStore, MongoDbStore<WorkflowDefinitionLabel> workflowDefinitionLabelMongoDbStore)
    {
        _labelMongoDbStore = labelMongoDbStore;
        _workflowDefinitionLabelMongoDbStore = workflowDefinitionLabelMongoDbStore;
    }

    /// <inheritdoc />
    public async Task<Label?> FindByIdAsync(string id, CancellationToken cancellationToken = default) => 
        await _labelMongoDbStore.FindAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<Label>> FindManyByIdAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => 
        await _labelMongoDbStore.FindManyAsync(x => ids.ToList().Contains(x.Id), cancellationToken);

    /// <inheritdoc />
    public async Task<Page<Label>> ListAsync(PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        if (pageArgs?.Page is null || pageArgs.PageSize is null)
        {
            var allDocuments = await _labelMongoDbStore.GetCollection().AsQueryable().ToListAsync(cancellationToken);
            return Page.Of(allDocuments, allDocuments.Count);
        }
        
        var count = await _labelMongoDbStore.GetCollection().AsQueryable().LongCountAsync(cancellationToken);
        var documents = (await _labelMongoDbStore.FindManyAsync(query => Paginate(query, pageArgs), cancellationToken)).ToList();
        return Page.Of(documents, count);
    }

    /// <inheritdoc />
    public async Task SaveAsync(Label record, CancellationToken cancellationToken = default) => 
        await _labelMongoDbStore.SaveAsync(record, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<Label> records, CancellationToken cancellationToken = default) => 
        await _labelMongoDbStore.SaveManyAsync(records, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await _workflowDefinitionLabelMongoDbStore.DeleteWhereAsync(x => x.LabelId == id, cancellationToken);
        return await _labelMongoDbStore.DeleteWhereAsync(x => x.Id == id, cancellationToken) > 0;
    }

    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        await _workflowDefinitionLabelMongoDbStore.DeleteWhereAsync(x => idList.Contains(x.LabelId), cancellationToken);
        return await _labelMongoDbStore.DeleteWhereAsync(x => idList.Contains(x.Id), cancellationToken);
    }
    
    private static IMongoQueryable<Label> Paginate(IMongoQueryable<Label> queryable, PageArgs pageArgs) => 
        (queryable.Paginate(pageArgs) as IMongoQueryable<Label>)!;
}