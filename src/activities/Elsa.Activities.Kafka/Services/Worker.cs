using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Elsa.Activities.Kafka.Activities.KafkaMessageReceived;
using Elsa.Activities.Kafka.Bookmarks;
using Elsa.Activities.Kafka.Configuration;
using Elsa.Activities.Kafka.Models;
using Elsa.Activities.Kafka.SchemaRegistry;
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
        private readonly IKafkaCustomActivityProvider? _kafkaCustomActivityProvider;


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
            ILogger<Worker> logger,
            IKafkaCustomActivityProvider? kafkaCustomActivityProvider)
        {
            Topic = topic;
            Group = group;
            _scopeFactory = scopeFactory;
            _client = client;
            _disposeReceiverAction = disposeReceiverAction;
            _logger = logger;
            _tenantIdResolver = tenantIdResolver;
            _kafkaCustomActivityProvider = kafkaCustomActivityProvider;

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
            _logger.LogError($"Error on consuming: {e.Message}");
            if (!(e.GetType() == typeof(ObjectDisposedException))) // This line prevents an infinite loop when unpublishing a KafkaMessageReceived Trigger. Probably a SemaphoreSlim problem.
            {
                await _disposeReceiverAction(this, _client);
            }
        }

        private async Task TriggerWorkflowsAsync(KafkaMessageEvent ev, CancellationToken cancellationToken)
        {
            //avoid handler being triggered earlier than workflow is suspended
            await Task.Delay(_delay, cancellationToken);
            using var scope = _scopeFactory.CreateScope();
            var config = _client.Configuration;
            var tenantId = await _tenantIdResolver.ResolveAsync(ev, config.Topic, config.Group, Tags, cancellationToken);
            var workflowInput = new WorkflowInput(new MessageReceivedInput() { MessageBytes = ev.Message.Value, MessageString = Encoding.ASCII.GetString(ev.Message.Value) });

            // Schema extraction if injected
            var schemaResolver = scope.ServiceProvider.GetRequiredService<ISchemaResolver>();
            var schema = await schemaResolver.ResolveSchemaForMessage(ev.Message);

            var bookmark = new MessageReceivedBookmark(config.ConnectionString, config.Topic, config.Group, GetHeaders(ev.Message.Headers, config.IgnoreHeaders), config.AutoOffsetReset, schema, config.IgnoreHeaders);
            var launchContext = new WorkflowsQuery(ActivityType, bookmark, TenantId: tenantId);

            // Launch KafkaMessageReceived activity
            var workflowLaunchpad = scope.ServiceProvider.GetRequiredService<IWorkflowLaunchpad>();
            await workflowLaunchpad.CollectAndDispatchWorkflowsAsync(launchContext, workflowInput, cancellationToken);

            // Launch all activities where the trigger inherits from ActivityType
            if (_kafkaCustomActivityProvider != null && _kafkaCustomActivityProvider.KafkaOverrideTriggers != null)
            {
                foreach (var t in _kafkaCustomActivityProvider.KafkaOverrideTriggers)
                {
                    var customLaunchContext = new WorkflowsQuery(t ?? "", bookmark, TenantId: tenantId);
                    await workflowLaunchpad.CollectAndDispatchWorkflowsAsync(customLaunchContext, workflowInput, cancellationToken);
                }
            }
        }

        private Dictionary<string, string> GetHeaders(Headers headers, bool ignoreHeaders)
        {
            var result = new Dictionary<string, string>();
            if (ignoreHeaders)
                return result;

            foreach (var header in headers)
            {
                result.Add(header.Key, Encoding.UTF8.GetString(header.GetValueBytes()));
            }

            return result;
        }
    }
}
