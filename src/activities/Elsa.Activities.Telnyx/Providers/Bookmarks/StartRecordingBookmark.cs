using Elsa.Activities.Telnyx.Activities;
using Elsa.Bookmarks;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public class StartRecordingBookmark : IBookmark
    {
    }
    
    public class StartRecordingBookmarkProvider : DefaultBookmarkProvider<StartRecordingBookmark, StartRecording>
    {
    }
}