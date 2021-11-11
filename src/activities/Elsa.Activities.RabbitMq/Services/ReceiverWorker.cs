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
            ILogger<WorkerBase> logger): base (receiverClient, disposeReceiverAction, logger)
        {
            _workflowLaunchpad = workflowLaunchpad;

            var onMessageReceived = async (TransportMessage message, CancellationToken cancellationToken) => await TriggerWorkflowsAsync(message, cancellationToken);

            _client.StartWithHandler(onMessageReceived);
        }

        private async Task TriggerWorkflowsAsync(TransportMessage message, CancellationToken cancellationToken)
        {
            var config = _client.Configuration;

            var bookmark = new MessageReceivedBookmark(config.RoutingKey, config.ConnectionString, config.Headers);
            var launchContext = new WorkflowsQuery(ActivityType, bookmark);

            await _workflowLaunchpad.UseServiceAsync(service => service.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(message), cancellationToken));
        }
    }
}