using Elsa.Activities.Telnyx.Activities;
using Elsa.Bookmarks;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public class BridgeCallsBookmark : IBookmark
    {
    }
    
    public class BridgeCallsBookmarkProvider : DefaultBookmarkProvider<BridgeCallsBookmark, BridgeCalls>
    {
    }
}