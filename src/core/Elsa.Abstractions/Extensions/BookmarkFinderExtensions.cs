using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Bookmarks;

namespace Elsa
{
    public static class BookmarkFinderExtensions
    {
        public static Task<IEnumerable<BookmarkFinderResult>> FindBookmarksAsync<T>(
            this IBookmarkFinder bookmarkFinder,
            IEnumerable<IBookmark> bookmarks,
            string? correlationId = default,
            string? tenantId = default,
            CancellationToken cancellationToken = default) where T : IActivity =>
            bookmarkFinder.FindBookmarksAsync(typeof(T).Name, bookmarks, correlationId, tenantId, cancellationToken);

        public static Task<IEnumerable<BookmarkFinderResult>> FindBookmarksAsync<T>(
            this IBookmarkFinder bookmarkFinder,
            IBookmark bookmark,
            string? correlationId = default,
            string? tenantId = default,
            CancellationToken cancellationToken = default) where T : IActivity =>
            bookmarkFinder.FindBookmarksAsync(typeof(T).Name, new[] { bookmark }, correlationId, tenantId, cancellationToken);

        public static Task<IEnumerable<BookmarkFinderResult>> FindBookmarksAsync(
            this IBookmarkFinder bookmarkFinder,
            string activityType,
            IBookmark bookmark,
            string? correlationId = default,
            string? tenantId = default,
            CancellationToken cancellationToken = default) =>
            bookmarkFinder.FindBookmarksAsync(activityType, new[] { bookmark }, correlationId, tenantId, cancellationToken);

        public static Task<IEnumerable<BookmarkFinderResult>> FindBookmarksAsync<T>(
            this IBookmarkFinder bookmarkFinder,
            string? correlationId = default,
            string? tenantId = default,
            CancellationToken cancellationToken = default) where T : IActivity =>
            bookmarkFinder.FindBookmarksAsync(typeof(T).Name, Enumerable.Empty<IBookmark>(), correlationId, tenantId, cancellationToken);
    }
}