using Elsa.Services.Bookmarks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.File.Bookmarks
{
    public class WatchDirectoryBookmark : IBookmark
    {
        public WatchDirectoryBookmark()
        { }

        public WatchDirectoryBookmark(string path)
        {
            Path = path;
        }

        public string Path { get; set; }
    }

    public class WatchDirectoryBookmarkProvider : BookmarkProvider<WatchDirectoryBookmark, WatchDirectory>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<WatchDirectory> context, CancellationToken cancellationToken) =>
            new[]
            {
                Result(new WatchDirectoryBookmark()
                {
                    Path = (await context.ReadActivityPropertyAsync(a => a.Path))
                })
            };
    }
}
