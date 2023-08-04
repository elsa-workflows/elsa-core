using System.Linq.Expressions;
using Elsa.Extensions;
using Elsa.MongoDb.Extensions;
using JetBrains.Annotations;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDb.Common;

/// <summary>
/// A generic repository class around MongoDb for accessing documents.
/// </summary>
/// <typeparam name="TDocument">The type of the document.</typeparam>
[PublicAPI]
public class MongoDbStore<TDocument> where TDocument : class
{
    private readonly IMongoCollection<TDocument> _collection;
    
    /// <param name="collection"></param>
    public MongoDbStore(IMongoCollection<TDocument> collection)
    {
        _collection = collection;
    }
    
    /// <summary>
    /// Returns a queryable collection of documents.
    /// </summary>
    public IMongoCollection<TDocument> GetCollection() => _collection;
    
    /// <summary>
    /// Saves the document.
    /// </summary>
    /// <param name="document">The document to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<TDocument> AddAsync(TDocument document, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(document, new InsertOneOptions(), cancellationToken);
        return document;
    }
    
    /// <summary>
    /// Saves a list of documents.
    /// </summary>
    /// <param name="documents">The documents to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task AddManyAsync(IEnumerable<TDocument> documents, CancellationToken cancellationToken = default)
    {
        await _collection.InsertManyAsync(documents, new InsertManyOptions(), cancellationToken);
    }
    
    /// <summary>
    /// Saves the document.
    /// </summary>
    /// <param name="document">The document to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<TDocument> SaveAsync(TDocument document, CancellationToken cancellationToken = default)
    {
        return await _collection.FindOneAndReplaceAsync(document.BuildIdFilter(), document, new FindOneAndReplaceOptions<TDocument>{ ReturnDocument = ReturnDocument.After, IsUpsert = true }, cancellationToken);
    }
    
    /// <summary>
    /// Saves the document.
    /// </summary>
    /// <param name="document">The document to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<TDocument> SaveAsync<TResult>(TDocument document, Expression<Func<TDocument, TResult>> selector, CancellationToken cancellationToken = default)
    {
        return await _collection.FindOneAndReplaceAsync(document.BuildExpression(selector), document, new FindOneAndReplaceOptions<TDocument>{ ReturnDocument = ReturnDocument.After, IsUpsert = true }, cancellationToken);
    }
    
    /// <summary>
    /// Saves the specified documents.
    /// </summary>
    /// <param name="documents">The documents to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveManyAsync(IEnumerable<TDocument> documents, CancellationToken cancellationToken = default)
    {
        var writes = new List<WriteModel<TDocument>>();

        foreach (var document in documents)
        {
            var replacement = new ReplaceOneModel<TDocument>(document.BuildIdFilter(), document) { IsUpsert = true };
            writes.Add(replacement);
        }

        await _collection.BulkWriteAsync(writes, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Finds the document matching the specified predicate
    /// </summary>
    /// <param name="predicate">The predicate to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The document if found, otherwise <c>null</c>.</returns>
    public async Task<TDocument?> FindAsync(Expression<Func<TDocument, bool>> predicate, CancellationToken cancellationToken = default) => 
        await _collection.AsQueryable().Where(predicate).FirstOrDefaultAsync(cancellationToken);
    
    /// <summary>
    /// Finds a single document using a query
    /// </summary>
    /// <param name="query">The query to use</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The document if found, otherwise <c>null</c></returns>
    public async Task<TDocument?> FindAsync(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, CancellationToken cancellationToken = default) => 
        await query(_collection.AsQueryable()).FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Finds a list of documents matching the specified predicate
    /// </summary>
    public async Task<IEnumerable<TDocument>> FindManyAsync(Expression<Func<TDocument, bool>> predicate, CancellationToken cancellationToken = default) => 
        await _collection.AsQueryable().Where(predicate).ToListAsync(cancellationToken);
    
    /// <summary>
    /// Queries the database using a query and a selector.
    /// </summary>
    public async Task<IEnumerable<TResult>> FindManyAsync<TResult>(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, Expression<Func<TDocument, TResult>> selector, CancellationToken cancellationToken = default) => 
        await query(_collection.AsQueryable()).Select(selector).ToListAsync(cancellationToken);

    /// <summary>
    /// Finds a list of documents using a query
    /// </summary>
    public async Task<IEnumerable<TDocument>> FindManyAsync(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, CancellationToken cancellationToken = default) => 
        await query(_collection.AsQueryable()).ToListAsync(cancellationToken);
    
    /// <summary>
    /// Queries the database using a query and a selector.
    /// </summary>
    public async Task<IEnumerable<TResult>> FindMany<TResult>(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, Expression<Func<TDocument, TResult>> selector, CancellationToken cancellationToken = default) => 
        await query(_collection.AsQueryable()).Select(selector).ToListAsync(cancellationToken);
    
    /// <summary>
    /// Counts documents in the collection using a filter.
    /// </summary>
    public async Task<long> CountAsync(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, CancellationToken cancellationToken = default) => 
        await query(_collection.AsQueryable()).LongCountAsync(cancellationToken);
    
    /// <summary>
    /// Counts documents in the collection using a filter and distinct by a key selector.
    /// </summary>
    public async Task<long> CountAsync<TProperty>(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, Expression<Func<TDocument, TProperty>> propertySelector, CancellationToken cancellationToken = default) => 
        await query((IMongoQueryable<TDocument>)_collection.AsQueryable().DistinctBy(propertySelector)).LongCountAsync(cancellationToken);
    
    /// <summary>
    /// Checks if any documents exist.
    /// </summary>
    public async Task<bool> AnyAsync(Expression<Func<TDocument, bool>> predicate, CancellationToken cancellationToken = default) => 
        await _collection.AsQueryable().Where(predicate).AnyAsync(cancellationToken);
    
    /// <summary>
    /// Deletes documents using a predicate.
    /// </summary>
    /// <returns>The number of documents deleted.</returns>
    public async Task<long> DeleteWhereAsync(Expression<Func<TDocument, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var documentsToDelete = await _collection.AsQueryable().Where(predicate).ToListAsync(cancellationToken);
        var count = documentsToDelete.LongCount();
    
        await _collection.DeleteManyAsync(predicate, cancellationToken);

        return count;
    }
    
    /// <summary>
    /// Deletes documents using a query.
    /// </summary>
    /// <returns>The number of documents deleted.</returns>
    public async Task<long> DeleteWhereAsync<TKey>(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, Expression<Func<TDocument, TKey>> keySelector, CancellationToken cancellationToken = default)
    {
        var key = keySelector.GetPropertyName();
        return await DeleteWhereAsync(query, key, cancellationToken);
    }
    
    /// <summary>
    /// Deletes documents using a query.
    /// </summary>
    /// <returns>The number of documents deleted.</returns>
    public async Task<long> DeleteWhereAsync(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, string key = "Id", CancellationToken cancellationToken = default)
    {
        var documentsToDelete = await query(_collection.AsQueryable()).ToListAsync(cancellationToken);
        var count = documentsToDelete.LongCount();
        var filter = documentsToDelete.BuildIdFilterForList(key);
        await _collection.DeleteManyAsync(filter, cancellationToken);

        return count;
    }
}