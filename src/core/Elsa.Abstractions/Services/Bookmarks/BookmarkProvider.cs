using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services
{
    public abstract class BookmarkProvider<T> : IBookmarkProvider where T : IBookmark
    {
        public virtual ValueTask<bool> SupportsActivityAsync(BookmarkProviderContext context, CancellationToken cancellationToken = default) => new(SupportsActivity(context));
        public virtual bool SupportsActivity(BookmarkProviderContext context) => false;
        public virtual ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext context, CancellationToken cancellationToken) => new(GetBookmarks(context));
        public virtual IEnumerable<BookmarkResult> GetBookmarks(BookmarkProviderContext context) => Enumerable.Empty<BookmarkResult>();

        async ValueTask<bool> IBookmarkProvider.SupportsActivityAsync(BookmarkProviderContext context, CancellationToken cancellationToken) =>
            await SupportsActivityAsync(new BookmarkProviderContext(context.ActivityExecutionContext, context.ActivityType, context.Mode), cancellationToken);

        async ValueTask<IEnumerable<BookmarkResult>> IBookmarkProvider.GetBookmarksAsync(BookmarkProviderContext context, CancellationToken cancellationToken)
        {
            var isSupported = await SupportsActivityAsync(context, cancellationToken);

            if (!isSupported)
                return Array.Empty<BookmarkResult>();

            return await GetBookmarksAsync(context, cancellationToken);
        }

        protected BookmarkResult Result(T bookmark, string? activityTypeName = default) => new(bookmark, activityTypeName);
    }

    public abstract class BookmarkProvider<T, TActivity> : IBookmarkProvider where T : IBookmark where TActivity : IActivity
    {
        public virtual ValueTask<bool> SupportsActivityAsync(BookmarkProviderContext<TActivity> context, CancellationToken cancellationToken = default) => new(SupportsActivity(context));
        public virtual bool SupportsActivity(BookmarkProviderContext<TActivity> context) => context.ActivityExecutionContext.ActivityBlueprint.Type == typeof(TActivity).Name;
        public virtual ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<TActivity> context, CancellationToken cancellationToken) => new(GetBookmarks(context));
        public virtual IEnumerable<BookmarkResult> GetBookmarks(BookmarkProviderContext<TActivity> context) => Enumerable.Empty<BookmarkResult>();

        async ValueTask<bool> IBookmarkProvider.SupportsActivityAsync(BookmarkProviderContext context, CancellationToken cancellationToken) =>
            await SupportsActivityAsync(new BookmarkProviderContext<TActivity>(context.ActivityExecutionContext, context.ActivityType, context.Mode), cancellationToken);

        async ValueTask<IEnumerable<BookmarkResult>> IBookmarkProvider.GetBookmarksAsync(BookmarkProviderContext context, CancellationToken cancellationToken)
        {
            var specializedContext = new BookmarkProviderContext<TActivity>(context.ActivityExecutionContext, context.ActivityType, context.Mode);
            var isSupported = await SupportsActivityAsync(specializedContext, cancellationToken);

            if (!isSupported)
                return Array.Empty<BookmarkResult>();

            return await GetBookmarksAsync(specializedContext, cancellationToken);
        }
        
        protected BookmarkResult Result(T bookmark, string? activityTypeName = default) => new(bookmark, activityTypeName);
    }
}