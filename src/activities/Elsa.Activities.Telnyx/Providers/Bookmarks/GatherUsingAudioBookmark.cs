using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Activities;
using Elsa.Activities.Telnyx.Extensions;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public class GatherUsingAudioBookmark : IBookmark
    {
        public string CallControlId { get; set; } = default!;
    }
    
    public class GatherUsingAudioBookmarkProvider : DefaultBookmarkProvider<GatherUsingAudioBookmark, GatherUsingAudio>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<GatherUsingAudio> context, CancellationToken cancellationToken)
        {
            var callControlId = (await context.ReadActivityPropertyAsync(x => x.CallControlId, cancellationToken))!;
            
            callControlId = context.ActivityExecutionContext.GetCallControlId(callControlId);
            
            var bookmark = new GatherUsingAudioBookmark
            {
                CallControlId = callControlId
            };

            var result = Result(bookmark);
            return new[] { result };
        }
    }
}