using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;
using Elsa.MongoDB.Common;
using Elsa.MongoDB.Extensions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDB.Stores.Labels;

public class MongoLabelStore : ILabelStore
{
    private readonly MongoStore<Label> _labelMongoStore;
    private readonly MongoStore<WorkflowDefinitionLabel> _workflowDefinitionLabelMongoStore;

    public MongoLabelStore(MongoStore<Label> labelMongoStore, MongoStore<WorkflowDefinitionLabel> workflowDefinitionLabelMongoStore)
    {
        _labelMongoStore = labelMongoStore;
        _workflowDefinitionLabelMongoStore = workflowDefinitionLabelMongoStore;
    }

    public async Task<Label?> FindByIdAsync(string id, CancellationToken cancellationToken = default) => 
        await _labelMongoStore.FindAsync(x => x.Id == id, cancellationToken);

    public async Task<IEnumerable<Label>> FindManyByIdAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => 
        await _labelMongoStore.FindManyAsync(x => ids.ToList().Contains(x.Id), cancellationToken);
    
    public async Task<Page<Label>> ListAsync(PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        if (pageArgs?.Page is null || pageArgs.PageSize is null)
        {
            var allDocuments = await _labelMongoStore.GetCollection().AsQueryable().ToListAsync(cancellationToken);
            return Page.Of(allDocuments, allDocuments.Count);
        }
        
        var count = await _labelMongoStore.GetCollection().AsQueryable().LongCountAsync(cancellationToken);
        var documents = (await _labelMongoStore.FindManyAsync(query => Paginate(query, pageArgs), cancellationToken)).ToList();
        return Page.Of(documents, count);
    }
    
    public async Task SaveAsync(Label record, CancellationToken cancellationToken = default) => 
        await _labelMongoStore.SaveAsync(record, cancellationToken);
    
    public async Task SaveManyAsync(IEnumerable<Label> records, CancellationToken cancellationToken = default) => 
        await _labelMongoStore.SaveManyAsync(records, cancellationToken);
    
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await _workflowDefinitionLabelMongoStore.DeleteWhereAsync(x => x.LabelId == id, cancellationToken);
        return await _labelMongoStore.DeleteWhereAsync(x => x.Id == id, cancellationToken) > 0;
    }

    public async Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        await _workflowDefinitionLabelMongoStore.DeleteWhereAsync(x => idList.Contains(x.LabelId), cancellationToken);
        return await _labelMongoStore.DeleteWhereAsync(x => idList.Contains(x.Id), cancellationToken);
    }
    
    private static IMongoQueryable<Label> Paginate(IMongoQueryable<Label> queryable, PageArgs pageArgs) => 
        (queryable.Paginate(pageArgs) as IMongoQueryable<Label>)!;
}