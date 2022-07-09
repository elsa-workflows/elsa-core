using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Newtonsoft.Json;

namespace Elsa.Activities.AzureServiceBus.Bookmarks
{
    public class MessageReceivedBookmark : IBookmark
    {
        [JsonConstructor]
        public MessageReceivedBookmark()
        {
        }

        public MessageReceivedBookmark(string queueOrTopic, string? subscription)
        {
            QueueOrTopic = queueOrTopic;
            Subscription = subscription;
        }

        public string QueueOrTopic { get; set; } = default!;
        public string? Subscription { get; set; }
    }

    public class MessageReceivedBookmarkProvider : BookmarkProvider<MessageReceivedBookmark>
    {
        public override bool SupportsActivity(BookmarkProviderContext context)
        {
            var activityType = context.ActivityExecutionContext.ActivityBlueprint.Type;
            return activityType is nameof(AzureServiceBusQueueMessageReceived) or nameof(AzureServiceBusTopicMessageReceived);
        }

        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext context, CancellationToken cancellationToken)
        {
            var activityType = context.ActivityExecutionContext.ActivityBlueprint.Type;

            var queueOrTopic = activityType == nameof(AzureServiceBusQueueMessageReceived)
                ? await context.ReadActivityPropertyAsync<AzureServiceBusQueueMessageReceived, string>(x => x.QueueName, cancellationToken)
                : await context.ReadActivityPropertyAsync<AzureServiceBusTopicMessageReceived, string>(x => x.TopicName, cancellationToken);

            var subscription = activityType == nameof(AzureServiceBusTopicMessageReceived)
                ? await context.ReadActivityPropertyAsync<AzureServiceBusTopicMessageReceived, string>(x => x.SubscriptionName, cancellationToken)
                : default;

            return new[] { Result(new MessageReceivedBookmark(queueOrTopic!, subscription)) };
        }
    }
}