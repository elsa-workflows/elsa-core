using Elsa.Activities.AzureServiceBus.Bookmarks;
using Elsa.Activities.AzureServiceBus.Options;
using Elsa.Bookmarks;
using Elsa.Dispatch;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public class TopicWorker : WorkerBase
    {
        public TopicWorker(
            IReceiverClient receiverClient,
            ICorrelatingWorkflowDispatcher correlatingWorkflowDispatcher,
            IOptions<AzureServiceBusOptions> options,
            ILogger<TopicWorker> logger) : base(receiverClient, correlatingWorkflowDispatcher, options, logger)
        {
        }

        protected override string ActivityType => nameof(AzureServiceBusTopicMessageReceived);

        protected override IBookmark CreateBookmark(Message message)
        {
            GetTopicAndSubscription(out var topicName, out var subscriptionName);
            return new TopicMessageReceivedBookmark(topicName, subscriptionName, message.CorrelationId);
        }

        protected override IBookmark CreateTrigger(Message message)
        {
            GetTopicAndSubscription(out var topicName, out var subscriptionName);
            return new TopicMessageReceivedBookmark(topicName, subscriptionName);
        }

        private void GetTopicAndSubscription(out string topicName, out string subscriptionName)
        {
            var segments = ReceiverClient.Path.Split('/');
            topicName = segments[0];
            subscriptionName = segments[2];
        }
    }
}