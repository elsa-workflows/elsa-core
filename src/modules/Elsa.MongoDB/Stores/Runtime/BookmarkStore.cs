using Elsa.MongoDB.Common;
using Elsa.MongoDB.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDB.Stores.Runtime;

/// <summary>
/// An MongoDb implementation of <see cref="IBookmarkStore"/>.
/// </summary>
public class MongoBookmarkStore : IBookmarkStore
{
    private readonly Store<Models.StoredBookmark> _store;
    private readonly IPayloadSerializer _serializer;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MongoBookmarkStore(Store<Models.StoredBookmark> store, IPayloadSerializer serializer)
    {
        _store = store;
        _serializer = serializer;
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default) => 
        await _store.SaveAsync(record.MapToDocument(_serializer), s => s.BookmarkId, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredBookmark>> FindManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default) => 
        (await _store.FindManyAsync(query => Filter(query, filter), cancellationToken)).Select(i => i.MapFromDocument(_serializer));

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(BookmarkFilter filter, CancellationToken cancellationToken = default) => 
        await _store.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);

    private IMongoQueryable<Models.StoredBookmark> Filter(IMongoQueryable<Models.StoredBookmark> queryable, BookmarkFilter filter) => 
        (filter.Apply(queryable.Select(i => i.MapFromDocument(_serializer))).Select(j => j.MapToDocument(_serializer)) as IMongoQueryable<Models.StoredBookmark>)!;
}