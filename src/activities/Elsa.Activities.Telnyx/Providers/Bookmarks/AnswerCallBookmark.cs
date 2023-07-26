using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Activities;
using Elsa.Activities.Telnyx.Extensions;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public class AnswerCallBookmark : IBookmark
    {
        public string CallControlId { get; set; } = default!;
    }

    public class AnswerCallBookmarkProvider : DefaultBookmarkProvider<AnswerCallBookmark, AnswerCall>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<AnswerCall> context, CancellationToken cancellationToken)
        {
            var callControlId = (await context.ReadActivityPropertyAsync(x => x.CallControlId, cancellationToken))!;
            
            callControlId = context.ActivityExecutionContext.GetCallControlId(callControlId);

            var bookmark = new AnswerCallBookmark
            {
                CallControlId = callControlId
            };

            var result = Result(bookmark);
            return new[] { result };
        }
    }
}