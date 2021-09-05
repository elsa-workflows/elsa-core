using Elsa.Services;
using System.Collections.Generic;
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
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<WatchDirectory> context, CancellationToken cancellationToken)
        {
            var path = await context.ReadActivityPropertyAsync(a => a.Path);
            var pattern = await context.ReadActivityPropertyAsync(a => a.Pattern);
            var result = Result(new FileCreatedBookmark(path, pattern));
            return new[] { result };
        }
    }
}