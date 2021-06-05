using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Activities;
using Elsa.Bookmarks;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public class AnswerCallBookmark : IBookmark
    {
    }

    public class AnswerCallBookmarkProvider : DefaultBookmarkProvider<AnswerCallBookmark, AnswerCall>
    {
    }
}