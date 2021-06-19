using Elsa.Activities.Telnyx.Activities;
using Elsa.Services.Bookmarks;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public class SpeakTextBookmark : IBookmark
    {
    }
    
    public class SpeakTextBookmarkProvider : DefaultBookmarkProvider<SpeakTextBookmark, SpeakText>
    {
    }
}