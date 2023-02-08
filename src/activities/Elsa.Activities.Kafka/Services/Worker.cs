using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Elsa.Activities.Kafka.Activities.KafkaMessageReceived;
using Elsa.Activities.Kafka.Bookmarks;
using Elsa.Activities.Kafka.Models;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.Kafka.Services
{
    public class Worker : IAsyncDisposable
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IClient _client;
        private readonly ILogger _logger;
        private readonly Func<Worker, IClient, Task> _disposeReceiverAction;
        private readonly IKafkaTenantIdResolver _tenantIdResolver;


        private string ActivityType => nameof(KafkaMessageReceived);
        private TimeSpan _delay = TimeSpan.FromMilliseconds(200);
        public string Topic { get; }
        public string Group { get; }

        public HashSet<string> Tags { get; } = new();

        public Worker(
            string topic,
            string group,
            IServiceScopeFactory scopeFactory,
            IClient client,
            Func<Worker, IClient, Task> disposeReceiverAction,
            IKafkaTenantIdResolver tenantIdResolver,
            ILogger<Worker> logger)
        {
            Topic = topic;
            Group = group;
            _scopeFactory = scopeFactory;
            _client = client;
            _disposeReceiverAction = disposeReceiverAction;
            _logger = logger;
            _tenantIdResolver = tenantIdResolver;

            _client.SetHandlers(OnMessageReceivedAsync, OnErrorAsync, new CancellationTokenSource().Token);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _client.StartProcessing(Topic, Group);
        }


        public async ValueTask DisposeAsync()
        {
            await Task.Run(() => { _client.Dispose(); });
        }

        private async Task OnMessageReceivedAsync(KafkaMessageEvent ev)
        {
            _logger.LogInformation("Message received for Topic {Topic}", _client.Configuration.Topic);

            await TriggerWorkflowsAsync(ev, ev.CancellationToken);
        }

        private async Task OnErrorAsync(Exception e)
        {
            _logger.LogError("Error on consuming");
            await _disposeReceiverAction(this, _client);
        }

        private async Task TriggerWorkflowsAsync(KafkaMessageEvent ev, CancellationToken cancellationToken)
        {
            //avoid handler being triggered earlier than workflow is suspended
            await Task.Delay(_delay, cancellationToken);
            var config = _client.Configuration;
            
            var tenantId = await _tenantIdResolver.ResolveAsync(ev, config.Topic, config.Group, Tags, cancellationToken);

            var bookmark = new MessageReceivedBookmark(config.ConnectionString, config.Topic, config.Group, GetHeaders(ev.Message.Headers), config.AutoOffsetReset);
            var launchContext = new WorkflowsQuery(ActivityType, bookmark, TenantId: tenantId);

            using var scope = _scopeFactory.CreateScope();
            var workflowLaunchpad = scope.ServiceProvider.GetRequiredService<IWorkflowLaunchpad>();
            await workflowLaunchpad.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(ev.Message.Value), cancellationToken);
        }

        private Dictionary<string, string> GetHeaders(Headers headers)
        {
            var result = new Dictionary<string, string>();
            foreach (var header in headers)
            {
                result.Add(header.Key, Encoding.UTF8.GetString(header.GetValueBytes()));
            }

            return result;
        }
    }
}