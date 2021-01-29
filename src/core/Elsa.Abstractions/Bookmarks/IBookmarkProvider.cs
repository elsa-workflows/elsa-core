using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Bookmarks
{
    public interface IBookmarkProvider
    {
        string ForActivityType { get; }
        ValueTask<IEnumerable<IBookmark>> GetBookmarksAsync(BookmarkProviderContext context, CancellationToken cancellationToken = default);
    }
}