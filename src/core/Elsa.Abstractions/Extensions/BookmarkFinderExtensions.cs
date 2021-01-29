using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using Elsa.Services;

namespace Elsa
{
    public static class BookmarkFinderExtensions
    {
        public static Task<IEnumerable<BookmarkFinderResult>> FindBookmarksAsync<T>(
            this IBookmarkFinder bookmarkFinder,
            IEnumerable<IBookmark> bookmarks,
            string? tenantId,
            CancellationToken cancellationToken = default) where T : IActivity =>
            bookmarkFinder.FindBookmarksAsync(typeof(T).Name, bookmarks, tenantId, cancellationToken);

        public static Task<IEnumerable<BookmarkFinderResult>> FindBookmarksAsync<T>(
            this IBookmarkFinder bookmarkFinder,
            IBookmark bookmark,
            string? tenantId,
            CancellationToken cancellationToken = default) where T : IActivity =>
            bookmarkFinder.FindBookmarksAsync(typeof(T).Name, new[] { bookmark }, tenantId, cancellationToken);
        
        public static Task<IEnumerable<BookmarkFinderResult>> FindBookmarksAsync(
            this IBookmarkFinder bookmarkFinder,
            string activityType,
            IBookmark bookmark,
            string? tenantId,
            CancellationToken cancellationToken = default) =>
            bookmarkFinder.FindBookmarksAsync(activityType, new[] { bookmark }, tenantId, cancellationToken);

        public static Task<IEnumerable<BookmarkFinderResult>> FindBookmarksAsync<T>(
            this IBookmarkFinder bookmarkFinder,
            string? tenantId,
            CancellationToken cancellationToken = default) where T : IActivity =>
            bookmarkFinder.FindBookmarksAsync(typeof(T).Name, Enumerable.Empty<IBookmark>(), tenantId, cancellationToken);
    }
}