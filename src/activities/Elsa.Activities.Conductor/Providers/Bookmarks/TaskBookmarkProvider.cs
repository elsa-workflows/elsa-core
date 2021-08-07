using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Bookmarks;

namespace Elsa.Activities.Conductor.Providers.Bookmarks
{
    public record TaskBookmark(string TaskName) : IBookmark
    {
    }
    
    public class TaskBookmarkProvider : BookmarkProvider<TaskBookmark, RunTask>
    {
        public override bool SupportsActivity(BookmarkProviderContext<RunTask> context) => context.ActivityType.Type == typeof(RunTask);
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<RunTask> context, CancellationToken cancellationToken) => await GetBookmarksInternalAsync(context, cancellationToken).ToListAsync(cancellationToken);

        private async IAsyncEnumerable<BookmarkResult> GetBookmarksInternalAsync(BookmarkProviderContext<RunTask> context, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var taskName = ToLower(await context.ReadActivityPropertyAsync(x => x.TaskName, cancellationToken))!;
            yield return Result(new TaskBookmark(taskName), nameof(RunTask));
        }

        private static string? ToLower(string? s) => s?.ToLowerInvariant();
    }
}