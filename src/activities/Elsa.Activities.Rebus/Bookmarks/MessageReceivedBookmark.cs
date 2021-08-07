using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Bookmarks;

namespace Elsa.Activities.Rebus.Bookmarks
{
    public class MessageReceivedBookmark : IBookmark
    {
        public string MessageType { get; set; } = default!;
    }

    public class MessageReceivedTriggerProvider : BookmarkProvider<MessageReceivedBookmark, RebusMessageReceived>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<RebusMessageReceived> context, CancellationToken cancellationToken) =>
            new[]
            {
                Result(new MessageReceivedBookmark
                {
                    MessageType = (await context.ReadActivityPropertyAsync(x => x.MessageType, cancellationToken))!.Name
                })
            };
    }
}