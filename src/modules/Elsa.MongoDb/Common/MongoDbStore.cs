using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Extensions;
using Elsa.MongoDb.Extensions;
using Elsa.Tenants;
using JetBrains.Annotations;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDb.Common;

/// <summary>
/// A generic repository class around MongoDb for accessing documents.
/// </summary>
/// <typeparam name="TDocument">The type of the document.</typeparam>
[PublicAPI]
public class MongoDbStore<TDocument>(IMongoCollection<TDocument> collection, ITenantResolver tenantResolver)
    where TDocument : class
{
    /// <summary>
    /// Returns a queryable collection of documents.
    /// </summary>
    public IMongoCollection<TDocument> GetCollection() => collection;

    /// <summary>
    /// Saves the document.
    /// </summary>
    /// <param name="document">The document to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<TDocument> AddAsync(TDocument document, CancellationToken cancellationToken = default)
    {
        await ApplyTenantIdAsync(document, cancellationToken);
        await collection.InsertOneAsync(document, new InsertOneOptions(), cancellationToken);
        return document;
    }

    /// <summary>
    /// Saves a list of documents.
    /// </summary>
    /// <param name="documents">The documents to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task AddManyAsync(IEnumerable<TDocument> documents, CancellationToken cancellationToken = default)
    {
        var documentsList = documents.ToList();
        await ApplyTenantIdAsync(documentsList, cancellationToken);
        await collection.InsertManyAsync(documentsList, new InsertManyOptions(), cancellationToken);
    }

    /// <summary>
    /// Saves the document.
    /// </summary>
    /// <param name="document">The document to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<TDocument> SaveAsync(TDocument document, CancellationToken cancellationToken = default)
    {
        await ApplyTenantIdAsync(document, cancellationToken);
        return await collection.FindOneAndReplaceAsync(document.BuildIdFilter(), document, new FindOneAndReplaceOptions<TDocument>
        {
            ReturnDocument = ReturnDocument.After,
            IsUpsert = true
        }, cancellationToken);
    }

    /// <summary>
    /// Saves the document.
    /// </summary>
    /// <param name="document">The document to save.</param>
    /// <param name="selector">The selector to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<TDocument> SaveAsync<TResult>(TDocument document, Expression<Func<TDocument, TResult>> selector, CancellationToken cancellationToken = default)
    {
        await ApplyTenantIdAsync(document, cancellationToken);
        return await collection.FindOneAndReplaceAsync(document.BuildExpression(selector), document, new FindOneAndReplaceOptions<TDocument>
        {
            ReturnDocument = ReturnDocument.After,
            IsUpsert = true
        }, cancellationToken);
    }

    /// <summary>
    /// Saves the specified documents.
    /// </summary>
    /// <param name="documents">The documents to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveManyAsync(IEnumerable<TDocument> documents, CancellationToken cancellationToken = default)
    {
        var documentsList = documents.ToList();
        await ApplyTenantIdAsync(documentsList, cancellationToken);
        var writes = new List<WriteModel<TDocument>>();

        foreach (var document in documentsList)
        {
            var replacement = new ReplaceOneModel<TDocument>(document.BuildIdFilter(), document)
            {
                IsUpsert = true
            };
            writes.Add(replacement);
        }

        if (!writes.Any())
            return;

        await collection.BulkWriteAsync(writes, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Saves the specified documents.
    /// </summary>
    /// <param name="documents">The documents to save.</param>
    /// <param name="primaryKey">The primary key to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveManyAsync(IEnumerable<TDocument> documents, string primaryKey = nameof(Entity.Id), CancellationToken cancellationToken = default)
    {
        var documentsList = documents.ToList();
        await ApplyTenantIdAsync(documentsList, cancellationToken);
        var writes = new List<WriteModel<TDocument>>();

        foreach (var document in documentsList)
        {
            var replacement = new ReplaceOneModel<TDocument>(document.BuildFilter(primaryKey), document)
            {
                IsUpsert = true
            };
            writes.Add(replacement);
        }

        if (!writes.Any())
            return;

        await collection.BulkWriteAsync(writes, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Finds the document matching the specified predicate
    /// </summary>
    /// <param name="predicate">The predicate to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The document if found, otherwise <c>null</c>.</returns>
    public async Task<TDocument?> FindAsync(Expression<Func<TDocument, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await FindAsync(predicate, false, cancellationToken);
    }

    /// <summary>
    /// Finds the document matching the specified predicate
    /// </summary>
    /// <param name="predicate">The predicate to use.</param>
    /// <param name="tenantAgnostic">Whether to include results across tenants.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The document if found, otherwise <c>null</c>.</returns>
    public async Task<TDocument?> FindAsync(Expression<Func<TDocument, bool>> predicate, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        var queryable = await GetQueryableCollectionAsync(tenantAgnostic, cancellationToken);
        return await queryable.Where(predicate).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Finds a single document using a query
    /// </summary>
    /// <param name="query">The query to use</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The document if found, otherwise <c>null</c></returns>
    public async Task<TDocument?> FindAsync(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, CancellationToken cancellationToken = default)
    {
        return await FindAsync(query, false, cancellationToken);
    }

    /// <summary>
    /// Finds a single document using a query
    /// </summary>
    /// <param name="query">The query to use</param>
    /// <param name="tenantAgnostic">Whether to include results across tenants</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The document if found, otherwise <c>null</c></returns>
    public async Task<TDocument?> FindAsync(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        var queryable = await GetQueryableCollectionAsync(tenantAgnostic, cancellationToken);
        return await query(queryable).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Finds a list of documents matching the specified predicate
    /// </summary>
    public async Task<IEnumerable<TDocument>> FindManyAsync(Expression<Func<TDocument, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync(predicate, false, cancellationToken);
    }

    /// <summary>
    /// Finds a list of documents matching the specified predicate
    /// </summary>
    public async Task<IEnumerable<TDocument>> FindManyAsync(Expression<Func<TDocument, bool>> predicate, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        var queryable = await GetQueryableCollectionAsync(tenantAgnostic, cancellationToken);
        return await queryable.Where(predicate).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Queries the database using a query and a selector.
    /// </summary>
    public async Task<IEnumerable<TResult>> FindManyAsync<TResult>(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, Expression<Func<TDocument, TResult>> selector, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync(query, selector, false, cancellationToken);
    }

    /// <summary>
    /// Queries the database using a query and a selector.
    /// </summary>
    public async Task<IEnumerable<TResult>> FindManyAsync<TResult>(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, Expression<Func<TDocument, TResult>> selector, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        var queryable = await GetQueryableCollectionAsync(tenantAgnostic, cancellationToken);
        return await query(queryable).Select(selector).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Finds a list of documents using a query
    /// </summary>
    public async Task<IEnumerable<TDocument>> FindManyAsync(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync(query, false, cancellationToken);
    }

    /// <summary>
    /// Finds a list of documents using a query
    /// </summary>
    public async Task<IEnumerable<TDocument>> FindManyAsync(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        var queryable = await GetQueryableCollectionAsync(tenantAgnostic, cancellationToken);
        return await query(queryable).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Queries the database using a query and a selector.
    /// </summary>
    public async Task<IEnumerable<TResult>> FindMany<TResult>(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, Expression<Func<TDocument, TResult>> selector, CancellationToken cancellationToken = default)
    {
        return await FindMany(query, selector, false, cancellationToken);
    }

    /// <summary>
    /// Queries the database using a query and a selector.
    /// </summary>
    public async Task<IEnumerable<TResult>> FindMany<TResult>(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, Expression<Func<TDocument, TResult>> selector, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        var queryable = await GetQueryableCollectionAsync(tenantAgnostic, cancellationToken);
        return await query(queryable).Select(selector).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Counts documents in the collection using a filter.
    /// </summary>
    public async Task<long> CountAsync(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, CancellationToken cancellationToken = default)
    {
        return await CountAsync(query, false, cancellationToken);
    }

    /// <summary>
    /// Counts documents in the collection using a filter.
    /// </summary>
    public async Task<long> CountAsync(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        var queryable = await GetQueryableCollectionAsync(tenantAgnostic, cancellationToken);
        return await query(queryable).LongCountAsync(cancellationToken);
    }

    /// <summary>
    /// Counts documents in the collection using a filter and distinct by a key selector.
    /// </summary>
    public async Task<long> CountAsync<TProperty>(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, Expression<Func<TDocument, TProperty>> propertySelector, CancellationToken cancellationToken = default)
    {
        return await CountAsync(query, propertySelector, false, cancellationToken);
    }

    /// <summary>
    /// Counts documents in the collection using a filter and distinct by a key selector.
    /// </summary>
    public async Task<long> CountAsync<TProperty>(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, Expression<Func<TDocument, TProperty>> propertySelector, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        var queryable = await GetQueryableCollectionAsync(tenantAgnostic, cancellationToken);
        return await query((IMongoQueryable<TDocument>)queryable.DistinctBy(propertySelector)).LongCountAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if any documents exist.
    /// </summary>
    public async Task<bool> AnyAsync(Expression<Func<TDocument, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await AnyAsync(predicate, false, cancellationToken);
    }

    /// <summary>
    /// Checks if any documents exist.
    /// </summary>
    public async Task<bool> AnyAsync(Expression<Func<TDocument, bool>> predicate, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        var queryable = await GetQueryableCollectionAsync(tenantAgnostic, cancellationToken);
        return await queryable.Where(predicate).AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes documents using a predicate.
    /// </summary>
    /// <returns>The number of documents deleted.</returns>
    public async Task<long> DeleteWhereAsync(Expression<Func<TDocument, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await DeleteWhereAsync(predicate, false, cancellationToken);
    }

    /// <summary>
    /// Deletes documents using a predicate.
    /// </summary>
    /// <returns>The number of documents deleted.</returns>
    public async Task<long> DeleteWhereAsync(Expression<Func<TDocument, bool>> predicate, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        return await DeleteWhereAsync(predicate, nameof(Entity.Id), tenantAgnostic, cancellationToken);
    }

    /// <summary>
    /// Deletes documents using a predicate.
    /// </summary>
    /// <returns>The number of documents deleted.</returns>
    public async Task<long> DeleteWhereAsync(Expression<Func<TDocument, bool>> predicate, string key, CancellationToken cancellationToken = default)
    {
        return await DeleteWhereAsync(predicate, key, false, cancellationToken);
    }

    /// <summary>
    /// Deletes documents using a predicate.
    /// </summary>
    /// <returns>The number of documents deleted.</returns>
    public async Task<long> DeleteWhereAsync(Expression<Func<TDocument, bool>> predicate, string key, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        var queryable = await GetQueryableCollectionAsync(tenantAgnostic, cancellationToken);
        var documentsToDelete = await queryable.Where(predicate).ToListAsync(cancellationToken);
        var count = documentsToDelete.LongCount();
        var filter = documentsToDelete.BuildIdFilterForList(key);
        await collection.DeleteManyAsync(filter, cancellationToken);

        return count;
    }

    /// <summary>
    /// Deletes documents using a query.
    /// </summary>
    /// <returns>The number of documents deleted.</returns>
    public async Task<long> DeleteWhereAsync<TKey>(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, Expression<Func<TDocument, TKey>> keySelector, CancellationToken cancellationToken = default)
    {
        return await DeleteWhereAsync(query, keySelector, false, cancellationToken);
    }

    /// <summary>
    /// Deletes documents using a query.
    /// </summary>
    /// <returns>The number of documents deleted.</returns>
    public async Task<long> DeleteWhereAsync<TKey>(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, Expression<Func<TDocument, TKey>> keySelector, bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        var key = keySelector.GetPropertyName();
        return await DeleteWhereAsync(query, key, tenantAgnostic, cancellationToken);
    }

    /// <summary>
    /// Deletes documents using a query.
    /// </summary>
    /// <returns>The number of documents deleted.</returns>
    public async Task<long> DeleteWhereAsync(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, string key = nameof(Entity.Id), CancellationToken cancellationToken = default)
    {
        return await DeleteWhereAsync(query, key, false, cancellationToken);
    }

    /// <summary>
    /// Deletes documents using a query.
    /// </summary>
    /// <returns>The number of documents deleted.</returns>
    public async Task<long> DeleteWhereAsync(Func<IMongoQueryable<TDocument>, IMongoQueryable<TDocument>> query, string key = nameof(Entity.Id), bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        var queryable = await GetQueryableCollectionAsync(tenantAgnostic, cancellationToken);
        var documentsToDelete = await query(queryable).ToListAsync(cancellationToken);
        var count = documentsToDelete.LongCount();
        var filter = documentsToDelete.BuildIdFilterForList(key);
        await collection.DeleteManyAsync(filter, cancellationToken);

        return count;
    }
    
    private async Task<IMongoQueryable<TDocument>> GetQueryableCollectionAsync(bool tenantAgnostic = false, CancellationToken cancellationToken = default)
    {
        var queryable = collection.AsQueryable();
        
        if(tenantAgnostic)
            return queryable;
        
        if(typeof(Entity).IsAssignableFrom(typeof(TDocument)))
        {
            var tenant = await tenantResolver.GetTenantAsync(cancellationToken);
            var tenantId = tenant?.Id;
            queryable = queryable.Where(x => (x as Entity)!.TenantId == tenantId);
        }
        
        return queryable;
    }

    private async Task ApplyTenantIdAsync(TDocument document, CancellationToken cancellationToken)
    {
        var tenant = await tenantResolver.GetTenantAsync(cancellationToken);
        var tenantId = tenant?.Id;

        if (document is Entity tenantDocument)
            tenantDocument.TenantId = tenantId;
    }

    private async Task ApplyTenantIdAsync(IEnumerable<TDocument> documents, CancellationToken cancellationToken)
    {
        var tenant = await tenantResolver.GetTenantAsync(cancellationToken);
        var tenantId = tenant?.Id;

        foreach (var document in documents)
        {
            if (document is Entity tenantDocument)
                tenantDocument.TenantId = tenantId;
        }
    }
}