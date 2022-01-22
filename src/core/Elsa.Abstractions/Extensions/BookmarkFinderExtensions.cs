using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;
using Rebus.Extensions;

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
            bookmarkFinder.FindBookmarksAsync(typeof(T).GetSimpleAssemblyQualifiedName(), bookmarks, correlationId, tenantId, cancellationToken);

        public static Task<IEnumerable<BookmarkFinderResult>> FindBookmarksAsync<T>(
            this IBookmarkFinder bookmarkFinder,
            IBookmark bookmark,
            string? correlationId = default,
            string? tenantId = default,
            CancellationToken cancellationToken = default) where T : IActivity =>
            bookmarkFinder.FindBookmarksAsync(typeof(T).GetSimpleAssemblyQualifiedName(), new[] { bookmark }, correlationId, tenantId, cancellationToken);

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
            bookmarkFinder.FindBookmarksAsync(typeof(T).GetSimpleAssemblyQualifiedName(), Enumerable.Empty<IBookmark>(), correlationId, tenantId, cancellationToken);

        public static Task<IEnumerable<Bookmark>> FindBookmarksByTypeAsync<T>(
            this IBookmarkFinder bookmarkFinder,
            string? tenantId = default,
            CancellationToken cancellationToken = default) where T : IBookmark =>
            bookmarkFinder.FindBookmarksByTypeAsync(typeof(T).GetSimpleAssemblyQualifiedName(), tenantId, cancellationToken);
    }
}