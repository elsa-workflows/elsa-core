using Elsa.Activities.Telnyx.Activities;
using Elsa.Services;
using Elsa.Services.Bookmarks;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public class StopAudioPlaybackBookmark : IBookmark
    {
    }
    
    public class StopAudioPlaybackBookmarkProvider : DefaultBookmarkProvider<StopAudioPlaybackBookmark, StopAudioPlayback>
    {
    }
}