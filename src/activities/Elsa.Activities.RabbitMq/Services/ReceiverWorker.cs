using Elsa.Activities.RabbitMq.Bookmarks;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;
using Rebus.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq.Services
{
    public class ReceiverWorker : WorkerBase
    {
        private readonly Scoped<IWorkflowLaunchpad> _workflowLaunchpad;
        private string ActivityType => nameof(RabbitMqMessageReceived);
        
        public ReceiverWorker(
            Scoped<IWorkflowLaunchpad> workflowLaunchpad,
            IClient receiverClient,
            Func<IClient, Task> disposeReceiverAction,
            ILogger<ReceiverWorker> logger): base (receiverClient, disposeReceiverAction, logger)
        {
            _workflowLaunchpad = workflowLaunchpad;

            var onMessageReceived = async (TransportMessage message, CancellationToken cancellationToken) => await TriggerWorkflowsAsync(message, cancellationToken);

            Client.StartWithHandler(onMessageReceived);
        }

        private async Task TriggerWorkflowsAsync(TransportMessage message, CancellationToken cancellationToken)
        {
            Logger.LogDebug("Message received for routing key {RoutingKey}", Client.Configuration.RoutingKey);

            var config = Client.Configuration;

            var bookmark = new MessageReceivedBookmark(config.RoutingKey, config.ConnectionString, config.Headers);
            var launchContext = new WorkflowsQuery(ActivityType, bookmark);

            await _workflowLaunchpad.UseServiceAsync(service => service.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(message), cancellationToken));
        }
    }
}