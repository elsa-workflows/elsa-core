using Elsa.Activities.RabbitMq.Configuration;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq.Services
{
    public class RabbitMqQueueStarter : IRabbitMqQueueStarter
    {
        private readonly IConfiguration _configuration;
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICollection<WorkerBase> _workers;
        private readonly IMessageReceiverClientFactory _messageReceiverClientFactory;
        private readonly IMessageSenderClientFactory _messageSenderClientFactory;
        private readonly ILogger _logger;

        public RabbitMqQueueStarter(
            IConfiguration configuration, 
            IMessageReceiverClientFactory messageReceiverClientFactory,
            IMessageSenderClientFactory messageSenderClientFactory,
            IServiceScopeFactory scopeFactory, 
            IServiceProvider serviceProvider, 
            ILogger<RabbitMqQueueStarter> logger)
        {
            _configuration = configuration;
            _messageReceiverClientFactory = messageReceiverClientFactory;
            _messageSenderClientFactory = messageSenderClientFactory;
            _scopeFactory = scopeFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _workers = new List<WorkerBase>();
        }

        public async Task CreateWorkersAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                await DisposeExistingWorkersAsync();

                var receiverConfigs = (await GetConfigurationsAsync<SendRabbitMqMessage>(null, cancellationToken).ToListAsync(cancellationToken)).Distinct();
                var senderConfigs = (await GetConfigurationsAsync<RabbitMqMessageReceived>(null, cancellationToken).ToListAsync(cancellationToken)).Distinct();

                foreach (var config in receiverConfigs)
                {
                    try
                    {
                        _workers.Add(await CreateReceiverWorkerAsync(config, cancellationToken));
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Failed to create a receiver for routing key {RoutingKey}", config.RoutingKey);
                    }
                }

                foreach (var config in senderConfigs)
                {
                    try
                    {
                        _workers.Add(await CreateSenderWorkerAsync(config, cancellationToken));
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Failed to create a sender for topic {RoutingKey}", config.RoutingKey);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<ReceiverWorker> CreateReceiverWorkerAsync(RabbitMqBusConfiguration config, CancellationToken cancellationToken = default)
        {
            var receiver = await _messageReceiverClientFactory.GetReceiverAsync(config, cancellationToken);
            return ActivatorUtilities.CreateInstance<ReceiverWorker>(_serviceProvider, (Func<IClient, Task>)DisposeReceiverAsync, receiver);
        }

        public async Task<SenderWorker> CreateSenderWorkerAsync(RabbitMqBusConfiguration config, CancellationToken cancellationToken = default)
        {
            var sender = await _messageSenderClientFactory.GetSenderAsync(config, cancellationToken);
            return ActivatorUtilities.CreateInstance<SenderWorker>(_serviceProvider, (Func<IClient, Task>)DisposeSenderAsync, sender);
        }

        private async Task DisposeReceiverAsync(IClient messageReceiver) => await _messageReceiverClientFactory.DisposeReceiverAsync(messageReceiver);
        private async Task DisposeSenderAsync(IClient messageSender) => await _messageSenderClientFactory.DisposeSenderAsync(messageSender);

        public async IAsyncEnumerable<RabbitMqBusConfiguration> GetConfigurationsAsync<T>(Func<IWorkflowBlueprint, bool>? predicate, [EnumeratorCancellation] CancellationToken cancellationToken) where T : IRabbitMqActivity
        {
            using var scope = _scopeFactory.CreateScope();
            var workflowRegistry = scope.ServiceProvider.GetRequiredService<IWorkflowRegistry>();
            var workflowBlueprintReflector = scope.ServiceProvider.GetRequiredService<IWorkflowBlueprintReflector>();
            var workflows = await workflowRegistry.ListActiveAsync(cancellationToken);

            var query =
                from workflow in workflows
                from activity in workflow.Activities
                where activity.Type == typeof(T).Name
                select workflow;

            var filteredQuery = predicate == null ? query : query.Where(predicate);

            foreach (var workflow in filteredQuery)
            {
                var workflowBlueprintWrapper = await workflowBlueprintReflector.ReflectAsync(scope.ServiceProvider, workflow, cancellationToken);

                foreach (var activity in workflowBlueprintWrapper.Filter<T>())
                {
                    var connectionString = await activity.EvaluatePropertyValueAsync(x => x.ConnectionString, cancellationToken);
                    var routingKey = await activity.EvaluatePropertyValueAsync(x => x.RoutingKey, cancellationToken);
                    var headers = await activity.EvaluatePropertyValueAsync(x => x.Headers, cancellationToken);

                    var config = new RabbitMqBusConfiguration(connectionString, routingKey, headers);

                    yield return config!;
                }
            }
        }
        private async Task DisposeExistingWorkersAsync()
        {
            foreach (var worker in _workers.ToList())
            {
                await worker.DisposeAsync();
                _workers.Remove(worker);
            }
        }
    }
}