using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Bookmarks
{
    public interface IBookmarkProvider
    {
        ValueTask<bool> SupportsActivityAsync(BookmarkProviderContext context, CancellationToken cancellationToken = default);
        ValueTask<IEnumerable<IBookmark>> GetBookmarksAsync(BookmarkProviderContext context, CancellationToken cancellationToken = default);
    }
}