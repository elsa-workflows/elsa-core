using Elsa.Common.Models;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Extensions;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;
using Open.Linq.AsyncExtensions;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// An EF Core implementation of <see cref="IBookmarkQueueStore"/>.
[UsedImplicitly]
public class EFBookmarkQueueStore(Store<RuntimeElsaDbContext, BookmarkQueueItem> store, IPayloadSerializer serializer) : IBookmarkQueueStore
{
    /// <inheritdoc />
    public Task SaveAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default)
    {
        return store.SaveAsync(record, s => s.Id, OnSaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public Task AddAsync(BookmarkQueueItem record, CancellationToken cancellationToken = default)
    {
        return store.AddAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public Task<BookmarkQueueItem?> FindAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default)
    {
        return store.FindAsync(filter.Apply, OnLoadAsync, cancellationToken);
    }

    public async Task<Page<BookmarkQueueItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
    {
        var count = await store.QueryAsync(queryable => queryable.OrderBy(orderBy), cancellationToken).LongCount();
        var results = await store.QueryAsync(queryable => queryable.Paginate(pageArgs), OnLoadAsync, cancellationToken).ToList();
        return new(results, count);
    }

    /// <inheritdoc />
    public async Task<long> DeleteAsync(BookmarkQueueFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteWhereAsync(filter.Apply, cancellationToken);
    }

    private ValueTask OnSaveAsync(RuntimeElsaDbContext dbContext, BookmarkQueueItem entity, CancellationToken cancellationToken)
    {
        dbContext.Entry(entity).Property("SerializedOptions").CurrentValue = entity.Options != null ? serializer.Serialize(entity.Options) : default;
        return default;
    }

    private ValueTask OnLoadAsync(RuntimeElsaDbContext dbContext, BookmarkQueueItem? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return default;

        var optionsJson = dbContext.Entry(entity).Property<string>("SerializedOptions").CurrentValue;
        entity.Options = !string.IsNullOrEmpty(optionsJson) ? serializer.Deserialize<ResumeBookmarkOptions>(optionsJson) : null;

        return default;
    }
}