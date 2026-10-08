using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class BookmarkQueueItemExtensions
{
    public static Task DeleteAsync(this IBookmarkQueueStore store, string id, CancellationToken cancellationToken = default)
    {
        var filter = new BookmarkQueueFilter
        {
            Id = id
        };

        return store.DeleteAsync(filter, cancellationToken);
    }
}