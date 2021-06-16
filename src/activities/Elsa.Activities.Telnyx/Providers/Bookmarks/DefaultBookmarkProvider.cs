using System.Collections.Generic;
using Elsa.Services;
using Elsa.Services.Bookmarks;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public abstract class DefaultBookmarkProvider<TBookmark, TActivity> : BookmarkProvider<TBookmark, TActivity> where TBookmark : IBookmark, new() where TActivity : IActivity
    {
        public override IEnumerable<BookmarkResult> GetBookmarks(BookmarkProviderContext<TActivity> context) => new[] {Result(new TBookmark())};
    }
}