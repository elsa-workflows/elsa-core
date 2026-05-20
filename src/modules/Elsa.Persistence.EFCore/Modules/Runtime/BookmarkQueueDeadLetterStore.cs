using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;
using Open.Linq.AsyncExtensions;

namespace Elsa.Persistence.EFCore.Modules.Runtime;

/// <summary>
/// An EF Core implementation of <see cref="IBookmarkQueueDeadLetterStore"/>.
/// </summary>
[UsedImplicitly]
public class EFBookmarkQueueDeadLetterStore(Store<RuntimeElsaDbContext, BookmarkQueueDeadLetterItem> store, IPayloadSerializer serializer) : IBookmarkQueueDeadLetterStore
{
    /// <inheritdoc />
    public Task SaveAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default)
    {
        return store.SaveAsync(record, s => s.Id, OnSaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public Task AddAsync(BookmarkQueueDeadLetterItem record, CancellationToken cancellationToken = default)
    {
        return store.AddAsync(record, OnSaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public Task<BookmarkQueueDeadLetterItem?> FindAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default)
    {
        return store.FindAsync(filter.Apply, OnLoadAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BookmarkQueueDeadLetterItem>> FindManyAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(filter.Apply, OnLoadAsync, filter.TenantAgnostic, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
    {
        var count = await store.QueryAsync(queryable => queryable, cancellationToken).LongCount();
        var results = await store.QueryAsync(queryable => queryable.OrderBy(orderBy).Paginate(pageArgs), OnLoadAsync, cancellationToken).ToList();
        return new(results, count);
    }

    /// <inheritdoc />
    public async Task<Page<BookmarkQueueDeadLetterItem>> PageAsync<TOrderBy>(PageArgs pageArgs, BookmarkQueueDeadLetterFilter filter, BookmarkQueueDeadLetterItemOrder<TOrderBy> orderBy, CancellationToken cancellationToken = default)
    {
        var count = await store.QueryAsync(filter.Apply, cancellationToken).LongCount();
        var results = await store.QueryAsync(queryable => filter.Apply(queryable).OrderBy(orderBy).Paginate(pageArgs), OnLoadAsync, cancellationToken).ToList();
        return new(results, count);
    }

    /// <inheritdoc />
    public async Task<long> DeleteAsync(BookmarkQueueDeadLetterFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteWhereAsync(filter.Apply, cancellationToken);
    }

    private ValueTask OnSaveAsync(RuntimeElsaDbContext dbContext, BookmarkQueueDeadLetterItem entity, CancellationToken cancellationToken)
    {
        dbContext.Entry(entity).Property("SerializedOptions").CurrentValue = entity.Options != null ? serializer.Serialize(entity.Options) : default;
        return default;
    }

    private ValueTask OnLoadAsync(RuntimeElsaDbContext dbContext, BookmarkQueueDeadLetterItem? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return default;

        var optionsJson = dbContext.Entry(entity).Property<string>("SerializedOptions").CurrentValue;
        entity.Options = !string.IsNullOrEmpty(optionsJson) ? serializer.Deserialize<ResumeBookmarkOptions>(optionsJson) : null;

        return default;
    }
}
