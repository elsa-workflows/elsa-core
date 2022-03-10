using Elsa.Activities.Telnyx.Activities;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public class StopAudioPlaybackBookmark : IBookmark
    {
    }
    
    public class StopAudioPlaybackBookmarkProvider : DefaultBookmarkProvider<StopAudioPlaybackBookmark, StopAudioPlayback>
    {
    }
}