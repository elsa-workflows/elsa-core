using Elsa.Bookmarks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.File.Bookmarks
{
    public record FileSystemChangedBookmark(WatcherChangeTypes ChangeType, string Directory, string Pattern) : IBookmark
    { }

    public class FileSystemChangedBookmarkProvider : BookmarkProvider<FileSystemChangedBookmark, WatchDirectory>
    {
        public override async ValueTask<IEnumerable<IBookmark>> GetBookmarksAsync(BookmarkProviderContext<WatchDirectory> context, CancellationToken cancellationToken) => new[]
        {
            new FileSystemChangedBookmark(
                ChangeType: context.Activity.GetPropertyValue(x => x.ChangeType),
                Directory: context.Activity.GetPropertyValue(x => x.Directory),
                Pattern: context.Activity.GetPropertyValue(x => x.Pattern))
        };
    }
}
