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
    public class Worker : IAsyncDisposable
    {
        private readonly IClient _client;
        private readonly ILogger _logger;
        private readonly Func<IClient, Task> _disposeReceiverAction;
        private readonly Scoped<IWorkflowLaunchpad> _workflowLaunchpad;
        private string ActivityType => nameof(RabbitMqMessageReceived);
        private TimeSpan _delay = TimeSpan.FromMilliseconds(200);

        public Worker(
            Scoped<IWorkflowLaunchpad> workflowLaunchpad,
            IClient client,
            Func<IClient, Task> disposeReceiverAction,
            ILogger<Worker> logger)
        {
            _client = client;
            _disposeReceiverAction = disposeReceiverAction;
            _logger = logger;
            _workflowLaunchpad = workflowLaunchpad;

            _client.SubscribeWithHandler(OnMessageReceived);
        }

        public async ValueTask DisposeAsync() => await _disposeReceiverAction(_client);

        private async Task OnMessageReceived(TransportMessage message, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Message received for routing key {RoutingKey}", _client.Configuration.RoutingKey);

            await TriggerWorkflowsAsync(message, cancellationToken);

            _client.StopClient();
        }

        private async Task TriggerWorkflowsAsync(TransportMessage message, CancellationToken cancellationToken)
        {
            //avoid handler being triggered earlier than workflow is suspended
            await Task.Delay(_delay, cancellationToken);

            var config = _client.Configuration;

            var bookmark = new MessageReceivedBookmark(config.ExchangeName, config.RoutingKey, config.ConnectionString, config.Headers);
            var launchContext = new WorkflowsQuery(ActivityType, bookmark);

            await _workflowLaunchpad.UseServiceAsync(service => service.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(message), cancellationToken));
        }
    }
}