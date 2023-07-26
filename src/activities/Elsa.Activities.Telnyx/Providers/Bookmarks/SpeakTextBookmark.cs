using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Activities;
using Elsa.Activities.Telnyx.Extensions;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public class SpeakTextBookmark : IBookmark
    {
        public string CallControlId { get; set; } = default!;
    }
    
    public class SpeakTextBookmarkProvider : DefaultBookmarkProvider<SpeakTextBookmark, SpeakText>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<SpeakText> context, CancellationToken cancellationToken)
        {
            var callControlId = (await context.ReadActivityPropertyAsync(x => x.CallControlId, cancellationToken))!;

            callControlId = context.ActivityExecutionContext.GetCallControlId(callControlId);
            
            var bookmark = new SpeakTextBookmark
            {
                CallControlId = callControlId
            };

            var result = Result(bookmark);
            return new[] { result };
        }
    }
}