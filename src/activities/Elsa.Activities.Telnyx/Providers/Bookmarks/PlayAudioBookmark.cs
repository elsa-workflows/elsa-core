using Elsa.Activities.Telnyx.Activities;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public class PlayAudioBookmark : IBookmark
    {
    }
    
    public class PlayAudioBookmarkProvider : DefaultBookmarkProvider<PlayAudioBookmark, PlayAudio>
    {
    }
}