using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

namespace Elsa.Activities.UserTask.Bookmarks
{
    using UserTask = Elsa.Activities.UserTask.Activities.UserTask;

    public record UserTaskBookmark(string Action) : IBookmark;

    public class UserTaskBookmarkProvider : BookmarkProvider<UserTaskBookmark, UserTask>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<UserTask> context, CancellationToken cancellationToken)
        {
            var actions = (await context.ReadActivityPropertyAsync(x => x.Actions, cancellationToken))!;
            return actions.Select(x => Result(new UserTaskBookmark(x))).ToList();
        }
    }
}