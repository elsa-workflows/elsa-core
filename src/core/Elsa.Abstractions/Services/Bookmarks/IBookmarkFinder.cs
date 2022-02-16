using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services
{
    public interface IBookmarkFinder
    {
        Task<IEnumerable<BookmarkFinderResult>> FindBookmarksAsync(
            string activityType,
            IEnumerable<IBookmark> bookmarks,
            string? correlationId = default,
            string? tenantId = default,
            int skip = 0,
            int take = int.MaxValue,
            CancellationToken cancellationToken = default);

        IAsyncEnumerable<BookmarkFinderResult> StreamBookmarksAsync(
            string activityType,
            IEnumerable<IBookmark> bookmarks,
            string? correlationId = default,
            string? tenantId = default,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<Bookmark>> FindBookmarksByTypeAsync(string bookmarkType, string? tenantId = default, int skip = 0, int take = int.MaxValue, CancellationToken cancellationToken = default);
        IAsyncEnumerable<Bookmark> StreamBookmarksByTypeAsync(string bookmarkType, string? tenantId = default, CancellationToken cancellationToken = default);
    }
}