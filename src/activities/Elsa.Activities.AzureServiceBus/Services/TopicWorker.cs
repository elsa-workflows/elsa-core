using System;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Bookmarks;
using Elsa.Activities.AzureServiceBus.Options;
using Elsa.Services;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public class TopicWorker : WorkerBase
    {
        public TopicWorker(
            string tag,
            IReceiverClient receiverClient,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<AzureServiceBusOptions> options,
            Func<IReceiverClient, Task> disposeReceiverAction,
            ILogger<TopicWorker> logger) : base(tag, receiverClient, serviceScopeFactory, options, disposeReceiverAction, logger)
        {
        }

        protected override string ActivityType => nameof(AzureServiceBusTopicMessageReceived);

        protected override IBookmark CreateBookmark(Message message)
        {
            GetTopicAndSubscription(out var topicName, out var subscriptionName);
            return new TopicMessageReceivedBookmark(topicName, subscriptionName);
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