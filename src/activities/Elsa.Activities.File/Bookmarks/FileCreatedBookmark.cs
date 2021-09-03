using Elsa.Services;
using Elsa.Services.Bookmarks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.File.Bookmarks
{
    public class FileCreatedBookmark : IBookmark
    {
        public FileCreatedBookmark()
        { }

        public FileCreatedBookmark(string path, string pattern)
        {
            Path = path;
            Pattern = pattern;
        }

        public string Path { get; set; }

        public string Pattern { get; set; }
    }

    public class FileCreatedBookmarkProvider : BookmarkProvider<FileCreatedBookmark, WatchDirectory>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<WatchDirectory> context, CancellationToken cancellationToken) =>
            new[]
            {
                Result(new FileCreatedBookmark()
                {
                    Path = await context.ReadActivityPropertyAsync(a => a.Path),
                    Pattern = await context.ReadActivityPropertyAsync(a => a.Pattern)
                })
            };
    }
}
