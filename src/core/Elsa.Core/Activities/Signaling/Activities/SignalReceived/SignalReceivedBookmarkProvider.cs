using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    public class SignalReceivedBookmark : IBookmark
    {
        public string Signal { get; set; } = default!;
    }

    public class SignalReceivedBookmarkProvider : BookmarkProvider<SignalReceivedBookmark, SignalReceived>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<SignalReceived> context, CancellationToken cancellationToken) => await GetBookmarksInternalAsync(context, cancellationToken).ToListAsync(cancellationToken);

        private async IAsyncEnumerable<BookmarkResult> GetBookmarksInternalAsync(BookmarkProviderContext<SignalReceived> context, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var signalName = (await context.ReadActivityPropertyAsync(x => x.Signal, cancellationToken))?.ToLowerInvariant().Trim();
            
            // Can't do anything with an empty signal name.
            if(string.IsNullOrEmpty(signalName))
                yield break;
            
            yield return Result(new SignalReceivedBookmark
            {
                Signal = signalName
            });
        }
    }
}