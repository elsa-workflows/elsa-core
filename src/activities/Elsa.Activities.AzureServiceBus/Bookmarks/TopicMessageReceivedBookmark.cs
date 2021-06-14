using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Bookmarks;

namespace Elsa.Activities.AzureServiceBus.Bookmarks
{
    public class TopicMessageReceivedBookmark : IBookmark
    {
        public TopicMessageReceivedBookmark()
        {
        }

        public TopicMessageReceivedBookmark(string topicName, string subscriptionName)
        {
            TopicName = topicName;
            SubscriptionName = subscriptionName;
        }

        public string TopicName { get; set; } = default!;
        public string SubscriptionName { get; set; } = default!;
    }

    public class TopicMessageReceivedBookmarkProvider : BookmarkProvider<TopicMessageReceivedBookmark, AzureServiceBusTopicMessageReceived>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<AzureServiceBusTopicMessageReceived> context, CancellationToken cancellationToken) =>
            new[]
            {
                Result(new TopicMessageReceivedBookmark
                {
                    TopicName = (await context.ReadActivityPropertyAsync(x => x.TopicName, cancellationToken))!,
                    SubscriptionName = (await context.ReadActivityPropertyAsync(x => x.SubscriptionName, cancellationToken))!,
                })
            };
    }
}