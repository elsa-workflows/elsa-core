using System;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Bookmarks;
using Elsa.Activities.AzureServiceBus.Options;
using Elsa.Services;
using Elsa.Services.Bookmarks;
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
            Scoped<IWorkflowLaunchpad> workflowLaunchpad,
            IOptions<AzureServiceBusOptions> options,
            Func<IReceiverClient, Task> disposeReceiverAction,
            ILogger<TopicWorker> logger) : base(receiverClient, workflowLaunchpad, options, disposeReceiverAction, logger)
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