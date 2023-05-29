using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Activities;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public class DialBookmark : IBookmark
    {
        public string CallControlId { get; set; } = default!;
    }

    public class DialBookmarkProvider : DefaultBookmarkProvider<DialBookmark, Dial>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<Dial> context, CancellationToken cancellationToken)
        {
            var dialResponse = await context.ReadActivityPropertyAsync(x => x.DialResponse, cancellationToken);
            var callControlId = dialResponse!.CallControlId;
            
            var bookmark = new DialBookmark
            {
                CallControlId = callControlId
            };

            var result = Result(bookmark);
            return new[] { result };
        }
    }
}