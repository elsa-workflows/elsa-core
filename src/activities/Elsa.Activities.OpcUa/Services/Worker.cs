using Elsa.Activities.OpcUa.Bookmarks;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Opc.Ua.Client;

namespace Elsa.Activities.OpcUa.Services
{
    public class Worker : IAsyncDisposable
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IClient _client;
        private readonly ILogger _logger;
        private readonly Func<IClient, Task> _disposeReceiverAction;
        private string ActivityType => nameof(OpcUaMessageReceived);
        private TimeSpan _delay = TimeSpan.FromMilliseconds(200);

        public Worker(
            IServiceScopeFactory scopeFactory,
            IClient client,
            Func<IClient, Task> disposeReceiverAction,
            ILogger<Worker> logger)
        {
            _scopeFactory = scopeFactory;
            _client = client;
            _disposeReceiverAction = disposeReceiverAction;
            _logger = logger;

            _client.SubscribeWithHandler(OnMessageReceived);
        }

        public async ValueTask DisposeAsync() => await _disposeReceiverAction(_client);

        private async Task OnMessageReceived(MonitoredItem message, CancellationToken cancellationToken)
        {
            await TriggerWorkflowsAsync(message, cancellationToken);
        }

        private async Task TriggerWorkflowsAsync(MonitoredItem message, CancellationToken cancellationToken)
        {
            //avoid handler being triggered earlier than workflow is suspended
            await Task.Delay(_delay, cancellationToken);

            var config = _client.Configuration;

            var bookmark = new MessageReceivedBookmark(config.ConnectionString, config.Tags);
            var launchContext = new WorkflowsQuery(ActivityType, bookmark);

            using var scope = _scopeFactory.CreateScope();
            var workflowLaunchpad = scope.ServiceProvider.GetRequiredService<IWorkflowLaunchpad>();
            await workflowLaunchpad.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(message), cancellationToken);
        }
    }
}