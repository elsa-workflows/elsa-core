using Elsa.Activities.Telnyx.Activities;
using Elsa.Bookmarks;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public class TransferCallBookmark : IBookmark
    {
    }
    
    public class TransferCallBookmarkProvider : DefaultBookmarkProvider<TransferCallBookmark, TransferCall>
    {
    }
}