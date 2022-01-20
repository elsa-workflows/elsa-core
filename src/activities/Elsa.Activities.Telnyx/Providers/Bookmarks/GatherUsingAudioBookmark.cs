using Elsa.Activities.Telnyx.Activities;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public class GatherUsingAudioBookmark : IBookmark
    {
    }
    
    public class GatherUsingAudioBookmarkProvider : DefaultBookmarkProvider<GatherUsingAudioBookmark, GatherUsingAudio>
    {
    }
}