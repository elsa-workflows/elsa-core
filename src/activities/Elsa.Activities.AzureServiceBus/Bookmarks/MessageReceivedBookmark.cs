using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

namespace Elsa.Activities.AzureServiceBus.Bookmarks
{
    public class MessageReceivedBookmark : IBookmark
    {
        public MessageReceivedBookmark()
        {
        }

        public MessageReceivedBookmark(string queueOrTopic, string? subscription)
        {
            QueueOrTopic = queueOrTopic;
            Subscription = subscription;
        }

        public string QueueOrTopic { get; set; } = default!;
        public string? Subscription { get; set; } = default!;
    }

    public class MessageReceivedBookmarkProvider : BookmarkProvider<MessageReceivedBookmark, AzureServiceBusMessageReceived>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<AzureServiceBusMessageReceived> context, CancellationToken cancellationToken) =>
            new[]
            {
                Result(new MessageReceivedBookmark
                {
                    QueueOrTopic = (await context.ReadActivityPropertyAsync(x => x.QueueOrTopic, cancellationToken))!,
                    Subscription = (await context.ReadActivityPropertyAsync(x => x.Subscription, cancellationToken))!,
                })
            };
    }
}