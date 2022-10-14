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
    public abstract class FilteredCallInitiatedBookmark : IBookmark
    {
        protected FilteredCallInitiatedBookmark()
        {
        }

        protected FilteredCallInitiatedBookmark(string phoneNumber)
        {
            PhoneNumber = phoneNumber;
        }

        public string PhoneNumber { get; set; } = default!;
    }

    public class FilteredCallInitiatedToBookmark : FilteredCallInitiatedBookmark
    {
        public FilteredCallInitiatedToBookmark()
        {
        }

        public FilteredCallInitiatedToBookmark(string phoneNumber) : base(phoneNumber)
        {
        }
    }

    public class FilteredCallInitiatedFromBookmark : FilteredCallInitiatedBookmark
    {
        public FilteredCallInitiatedFromBookmark()
        {
        }

        public FilteredCallInitiatedFromBookmark(string phoneNumber) : base(phoneNumber)
        {
        }
    }

    public class FilteredCallInitiatedBookmarkProvider : BookmarkProvider<FilteredCallInitiatedBookmark, FilteredCallInitiated>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<FilteredCallInitiated> context, CancellationToken cancellationToken)
        {
            var bookmarks = await CreateBookmarksAsync(context, cancellationToken).ToListAsync(cancellationToken);
            return bookmarks.Select(x => Result(x));
        }

        private async IAsyncEnumerable<FilteredCallInitiatedBookmark> CreateBookmarksAsync(BookmarkProviderContext<FilteredCallInitiated> context, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var to = (await context.ReadActivityPropertyAsync(x => x.To, cancellationToken) ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x) && x != "*");
            var from = (await context.ReadActivityPropertyAsync(x => x.From, cancellationToken) ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x) && x != "*");

            foreach (var number in to)
                yield return new FilteredCallInitiatedToBookmark(number);
            
            foreach (var number in from)
                yield return new FilteredCallInitiatedFromBookmark(number);
        }
    }
}