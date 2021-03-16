using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

namespace Elsa.Bookmarks
{
    public abstract class BookmarkProvider<T> : IBookmarkProvider where T : IBookmark
    {
        public virtual ValueTask<bool> SupportsActivityAsync(BookmarkProviderContext context, CancellationToken cancellationToken = default) => new(SupportsActivity(context));
        public virtual bool SupportsActivity(BookmarkProviderContext context) => false;
        public virtual ValueTask<IEnumerable<IBookmark>> GetBookmarksAsync(BookmarkProviderContext context, CancellationToken cancellationToken) => new(GetBookmarks(context));
        public virtual IEnumerable<IBookmark> GetBookmarks(BookmarkProviderContext context) => Enumerable.Empty<IBookmark>();

        async ValueTask<bool> IBookmarkProvider.SupportsActivityAsync(BookmarkProviderContext context, CancellationToken cancellationToken) =>
            await SupportsActivityAsync(new BookmarkProviderContext(context.ActivityExecutionContext, context.ActivityType, context.Mode), cancellationToken);

        async ValueTask<IEnumerable<IBookmark>> IBookmarkProvider.GetBookmarksAsync(BookmarkProviderContext context, CancellationToken cancellationToken)
        {
            var isSupported = await SupportsActivityAsync(context, cancellationToken);

            if (!isSupported)
                return new IBookmark[0];

            return await GetBookmarksAsync(context, cancellationToken);
        }
    }
    
    public abstract class BookmarkProvider<T, TActivity> : IBookmarkProvider where T : IBookmark where TActivity : IActivity
    {
        public virtual ValueTask<bool> SupportsActivityAsync(BookmarkProviderContext<TActivity> context, CancellationToken cancellationToken = default) => new(SupportsActivity(context));
        public virtual bool SupportsActivity(BookmarkProviderContext<TActivity> context) => context.ActivityExecutionContext.ActivityBlueprint.Type == typeof(TActivity).Name;
        public virtual ValueTask<IEnumerable<IBookmark>> GetBookmarksAsync(BookmarkProviderContext<TActivity> context, CancellationToken cancellationToken) => new(GetBookmarks(context));
        public virtual IEnumerable<IBookmark> GetBookmarks(BookmarkProviderContext<TActivity> context) => Enumerable.Empty<IBookmark>();

        async ValueTask<bool> IBookmarkProvider.SupportsActivityAsync(BookmarkProviderContext context, CancellationToken cancellationToken) =>
            await SupportsActivityAsync(new BookmarkProviderContext<TActivity>(context.ActivityExecutionContext, context.ActivityType, context.Mode), cancellationToken);

        async ValueTask<IEnumerable<IBookmark>> IBookmarkProvider.GetBookmarksAsync(BookmarkProviderContext context, CancellationToken cancellationToken)
        {
            var specializedContext = new BookmarkProviderContext<TActivity>(context.ActivityExecutionContext, context.ActivityType, context.Mode);
            var isSupported = await SupportsActivityAsync(specializedContext, cancellationToken);

            if (!isSupported)
                return new IBookmark[0];

            return await GetBookmarksAsync(specializedContext, cancellationToken);
        }
    }
}