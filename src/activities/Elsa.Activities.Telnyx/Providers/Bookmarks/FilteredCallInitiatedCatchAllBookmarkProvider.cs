using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Activities;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Providers.Bookmarks
{
    public class FilteredCallInitiatedCatchAllBookmark : IBookmark
    {
    }

    public class FilteredCallInitiatedCatchAllBookmarkProvider : BookmarkProvider<FilteredCallInitiatedCatchAllBookmark, FilteredCallInitiated>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<FilteredCallInitiated> context, CancellationToken cancellationToken)
        {
            var bookmarks = await CreateBookmarksAsync(context, cancellationToken).ToListAsync(cancellationToken);
            return bookmarks.Select(x => Result(x));
        }

        private async IAsyncEnumerable<FilteredCallInitiatedCatchAllBookmark> CreateBookmarksAsync(BookmarkProviderContext<FilteredCallInitiated> context, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var isCatchAll = (await context.ReadActivityPropertyAsync(x => x.To, cancellationToken) ?? Array.Empty<string>()).Contains("*");

            if(isCatchAll)
                yield return new FilteredCallInitiatedCatchAllBookmark();
        }
    }
}