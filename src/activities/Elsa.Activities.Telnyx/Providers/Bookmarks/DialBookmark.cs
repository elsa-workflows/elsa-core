using Elsa.Activities.Telnyx.Activities;
using Elsa.Bookmarks;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public class DialBookmark : IBookmark
    {
    }

    public class DialBookmarkProvider : DefaultBookmarkProvider<DialBookmark, Dial>
    {
    }
}