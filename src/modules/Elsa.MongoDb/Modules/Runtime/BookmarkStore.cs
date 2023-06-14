using Elsa.MongoDb.Common;
using Elsa.MongoDb.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDb.Modules.Runtime;

/// <summary>
/// An MongoDb implementation of <see cref="IBookmarkStore"/>.
/// </summary>
public class MongoBookmarkStore : IBookmarkStore
{
    private readonly MongoDbStore<StoredBookmark> _mongoDbStore;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MongoBookmarkStore(MongoDbStore<StoredBookmark> mongoDbStore)
    {
        _mongoDbStore = mongoDbStore;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default) => 
        await _mongoDbStore.SaveAsync(record, s => s.BookmarkId, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredBookmark>> FindManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default) => 
        (await _mongoDbStore.FindManyAsync(query => Filter(query, filter), cancellationToken));

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(BookmarkFilter filter, CancellationToken cancellationToken = default) => 
        await _mongoDbStore.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);

    private IMongoQueryable<StoredBookmark> Filter(IMongoQueryable<StoredBookmark> queryable, BookmarkFilter filter) => 
        (filter.Apply(queryable) as IMongoQueryable<StoredBookmark>)!;
}