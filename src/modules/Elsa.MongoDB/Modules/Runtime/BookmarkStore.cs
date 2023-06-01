using Elsa.MongoDB.Common;
using Elsa.MongoDB.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDB.Modules.Runtime;

/// <summary>
/// An MongoDb implementation of <see cref="IBookmarkStore"/>.
/// </summary>
public class MongoBookmarkStore : IBookmarkStore
{
    private readonly MongoStore<StoredBookmark> _mongoStore;
    private readonly IPayloadSerializer _serializer;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MongoBookmarkStore(MongoStore<StoredBookmark> mongoStore, IPayloadSerializer serializer)
    {
        _mongoStore = mongoStore;
        _serializer = serializer;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default) => 
        await _mongoStore.SaveAsync(record, s => s.BookmarkId, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredBookmark>> FindManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default) => 
        (await _mongoStore.FindManyAsync(query => Filter(query, filter), cancellationToken));

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(BookmarkFilter filter, CancellationToken cancellationToken = default) => 
        await _mongoStore.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);

    private IMongoQueryable<StoredBookmark> Filter(IMongoQueryable<StoredBookmark> queryable, BookmarkFilter filter) => 
        (filter.Apply(queryable) as IMongoQueryable<StoredBookmark>)!;
}