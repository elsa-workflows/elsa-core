using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Activities;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public class TransferCallBookmark : IBookmark
    {
        public string CallControlId { get; set; } = default!;
    }
    
    public class TransferCallBookmarkProvider : DefaultBookmarkProvider<TransferCallBookmark, TransferCall>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<TransferCall> context, CancellationToken cancellationToken)
        {
            var callControlId = (await context.ReadActivityPropertyAsync(x => x.CallControlId, cancellationToken))!;

            var bookmark = new TransferCallBookmark
            {
                CallControlId = callControlId
            };

            var result = Result(bookmark);
            return new[] { result };
        }
    }
}