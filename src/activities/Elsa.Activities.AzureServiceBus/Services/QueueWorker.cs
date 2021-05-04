using System;
using System.Threading.Tasks;
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
    public class QueueWorker : WorkerBase
    {
        public QueueWorker(
            IReceiverClient messageReceiver,
            IWorkflowDispatcher workflowDispatcher,
            IOptions<AzureServiceBusOptions> options,
            Func<IReceiverClient, Task> disposeReceiverAction,
            ILogger<QueueWorker> logger) : base(messageReceiver, workflowDispatcher, options, disposeReceiverAction, logger)
        {
        }

        protected override string ActivityType => nameof(AzureServiceBusQueueMessageReceived);

        protected override IBookmark CreateBookmark(Message message) => new QueueMessageReceivedBookmark(ReceiverClient.Path, message.CorrelationId);
        protected override IBookmark CreateTrigger(Message message) => new QueueMessageReceivedBookmark(ReceiverClient.Path);
    }
}