using Elsa.Attributes;
using Elsa.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.File.Bookmarks
{
    public class FileSystemEventBookmark : IBookmark
    {
        public FileSystemEventBookmark()
        { }

        public FileSystemEventBookmark(string? path, string? pattern, WatcherChangeTypes changeTypes, NotifyFilters notifyFilters)
        {
            ChangeTypes = changeTypes;
            NotifyFilters = notifyFilters;
            Path = path;
            Pattern = pattern;
        }

        [ExcludeFromHash]
        public WatcherChangeTypes ChangeTypes { get; set; }

        [ExcludeFromHash]
        public NotifyFilters NotifyFilters { get; set; }

        [ExcludeFromHash]
        public string? Path { get; set; }

        public string? Pattern { get; set; }

        public bool? Compare(IBookmark bookmark)
        {
            return bookmark is FileSystemEventBookmark other
                && ComparePaths(Path, other.Path)
                && ComparePaths(Pattern, other.Pattern)
                && ((NotifyFilters & other.NotifyFilters) > 0)
                && ((ChangeTypes & other.ChangeTypes) > 0);
        }

        private bool ComparePaths(string? left, string? right)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                return string.Equals(left, right);
            else
                return string.Equals(left, right, System.StringComparison.OrdinalIgnoreCase);
        }
    }

    public class FileCreatedBookmarkProvider : BookmarkProvider<FileSystemEventBookmark, WatchDirectory>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<WatchDirectory> context, CancellationToken cancellationToken)
        {
            var changeTypes = await context.ReadActivityPropertyAsync(a => a.ChangeTypes);
            var notifyFilters = await context.ReadActivityPropertyAsync(a => a.NotifyFilters);
            var path = await context.ReadActivityPropertyAsync(a => a.Path);
            var pattern = await context.ReadActivityPropertyAsync(a => a.Pattern);
            var result = Result(new FileSystemEventBookmark(path, pattern, changeTypes, notifyFilters));
            return new[] { result };
        }
    }
}