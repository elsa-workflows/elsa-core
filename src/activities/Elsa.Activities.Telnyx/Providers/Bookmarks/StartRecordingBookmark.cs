using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Activities;
using Elsa.Activities.Telnyx.Extensions;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public class StartRecordingBookmark : IBookmark
    {
        public string CallControlId { get; set; } = default!;
    }
    
    public class StartRecordingBookmarkProvider : DefaultBookmarkProvider<StartRecordingBookmark, StartRecording>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<StartRecording> context, CancellationToken cancellationToken)
        {
            var callControlId = (await context.ReadActivityPropertyAsync(x => x.CallControlId, cancellationToken))!;

            callControlId = context.ActivityExecutionContext.GetCallControlId(callControlId);
            
            var bookmark = new StartRecordingBookmark
            {
                CallControlId = callControlId
            };

            var result = Result(bookmark);
            return new[] { result };
        }
    }
}