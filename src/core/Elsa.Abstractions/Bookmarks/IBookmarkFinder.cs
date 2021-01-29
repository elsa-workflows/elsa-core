using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Bookmarks
{
    public interface IBookmarkFinder
    {
        Task<IEnumerable<BookmarkFinderResult>> FindBookmarksAsync(string activityType, IEnumerable<IBookmark> bookmarks, string? tenantId, CancellationToken cancellationToken = default);
    }
}