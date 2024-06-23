using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Filters;
using Elsa.MongoDb.Common;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDb.Modules.Alterations;

/// <summary>
/// A MongoDb implementation of <see cref="IAlterationJobStore"/>.
/// </summary>
public class MongoAlterationJobStore : IAlterationJobStore
{
    private readonly MongoDbStore<AlterationJob> _mongoDbStore;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MongoAlterationJobStore(MongoDbStore<AlterationJob> mongoDbStore)
    {
        _mongoDbStore = mongoDbStore;
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.CountAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AlterationJob?> FindAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.FindAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AlterationJob>> FindManyAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.FindManyAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> FindManyIdsAsync(AlterationJobFilter filter, CancellationToken cancellationToken = default)
    {
        var documents = await _mongoDbStore.FindManyAsync(query => Filter(query, filter), selector => selector.Id, cancellationToken);
        return documents;
    }

    /// <inheritdoc />
    public async Task SaveAsync(AlterationJob job, CancellationToken cancellationToken = default)
    {
        await _mongoDbStore.SaveAsync(job, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<AlterationJob> jobs, CancellationToken cancellationToken = default)
    {
        await _mongoDbStore.SaveManyAsync(jobs.Select(i => i), cancellationToken);
    }

    private static IMongoQueryable<AlterationJob> Filter(IMongoQueryable<AlterationJob> queryable, AlterationJobFilter filter) =>
        (filter.Apply(queryable) as IMongoQueryable<AlterationJob>)!;
}