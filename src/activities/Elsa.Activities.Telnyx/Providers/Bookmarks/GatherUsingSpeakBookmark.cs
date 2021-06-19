using Elsa.Activities.Telnyx.Activities;
using Elsa.Services.Bookmarks;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public class GatherUsingSpeakBookmark : IBookmark
    {
    }
    
    public class GatherUsingSpeakBookmarkProvider : DefaultBookmarkProvider<GatherUsingSpeakBookmark, GatherUsingSpeak>
    {
    }
}