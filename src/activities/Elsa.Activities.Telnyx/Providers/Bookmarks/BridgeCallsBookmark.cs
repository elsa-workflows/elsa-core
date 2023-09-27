using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Activities;
using Elsa.Activities.Telnyx.Extensions;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public class BridgeCallsBookmark : IBookmark
    {
        public string CallControlId { get; set; } = default!;
    }
    
    public class BridgeCallsBookmarkProvider : DefaultBookmarkProvider<BridgeCallsBookmark, BridgeCalls>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<BridgeCalls> context, CancellationToken cancellationToken)
        {
            var callControlIdA = (await context.ReadActivityPropertyAsync(x => x.CallControlIdA, cancellationToken))!;
            var callControlIdB = (await context.ReadActivityPropertyAsync(x => x.CallControlIdB, cancellationToken))!;
            
            return new[]
            {
                Result(new BridgeCallsBookmark { CallControlId = callControlIdA }),
                Result(new BridgeCallsBookmark { CallControlId = callControlIdB })
            };
        }
    }
}