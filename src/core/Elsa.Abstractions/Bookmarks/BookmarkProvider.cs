using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

namespace Elsa.Bookmarks
{
    public abstract class BookmarkProvider<T, TActivity> : IBookmarkProvider where T : IBookmark where TActivity : IActivity
    {
        public string ForActivityType => typeof(TActivity).Name;
        public virtual ValueTask<IEnumerable<IBookmark>> GetBookmarksAsync(BookmarkProviderContext<TActivity> context, CancellationToken cancellationToken) => new(GetBookmarks(context));
        public virtual IEnumerable<IBookmark> GetBookmarks(BookmarkProviderContext<TActivity> context) => Enumerable.Empty<IBookmark>();

        async ValueTask<IEnumerable<IBookmark>> IBookmarkProvider.GetBookmarksAsync(BookmarkProviderContext context, CancellationToken cancellationToken)
        {
            var supportedType = ForActivityType;
            if (context.ActivityExecutionContext.ActivityBlueprint.Type != supportedType)
                return new IBookmark[0];
            
            return await GetBookmarksAsync(new BookmarkProviderContext<TActivity>(context.ActivityExecutionContext, context.Mode), cancellationToken);
        }
    }
}