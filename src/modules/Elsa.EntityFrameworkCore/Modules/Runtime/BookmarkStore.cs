using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// An EF Core implementation of <see cref="IBookmarkStore"/>.
/// </summary>
public class EFCoreBookmarkStore : IBookmarkStore
{
    private readonly Store<RuntimeElsaDbContext, StoredBookmark> _store;
    
    /// <summary>
    /// Constructor.
    /// </summary>
    public EFCoreBookmarkStore(Store<RuntimeElsaDbContext, StoredBookmark> store) => _store = store;

    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, s => s.BookmarkId, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredBookmark>> FindManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default) => await _store.QueryAsync(filter.Apply, cancellationToken);

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(BookmarkFilter filter, CancellationToken cancellationToken = default) => await _store.DeleteWhereAsync(filter.Apply, cancellationToken);
}