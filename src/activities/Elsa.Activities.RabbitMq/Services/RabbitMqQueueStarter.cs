using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.RabbitMq.Configuration;
using Elsa.MultiTenancy;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.RabbitMq.Services
{
    public class RabbitMqQueueStarter : IRabbitMqQueueStarter
    {
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ICollection<Worker> _workers;
        private readonly IMessageReceiverClientFactory _messageReceiverClientFactory;
        private readonly ILogger _logger;
        private readonly ITenantStore _tenantStore;

        public RabbitMqQueueStarter(
            IMessageReceiverClientFactory messageReceiverClientFactory,
            IServiceScopeFactory scopeFactory, 
            ILogger<RabbitMqQueueStarter> logger,
            ITenantStore tenantStore)
        {
            _messageReceiverClientFactory = messageReceiverClientFactory;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _tenantStore = tenantStore;
            _workers = new List<Worker>();
        }

        public async Task CreateWorkersAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                await DisposeExistingWorkersAsync();

                foreach (var tenant in _tenantStore.GetTenants())
                {
                    using var scope = _scopeFactory.CreateScopeForTenant(tenant);

                    var receiverConfigs = (await GetConfigurationsAsync<RabbitMqMessageReceived>(null, scope.ServiceProvider, cancellationToken).ToListAsync(cancellationToken)).GroupBy(c => c.GetHashCode()).Select(x => x.First());

                    foreach (var config in receiverConfigs)
                    {
                        try
                        {
                            _workers.Add(await CreateWorkerAsync(scope.ServiceProvider, config, cancellationToken));
                        }
                        catch (Exception e)
                        {
                            _logger.LogWarning(e, "Failed to create a receiver for routing key {RoutingKey}", config.RoutingKey);
                        }
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async IAsyncEnumerable<RabbitMqBusConfiguration> GetConfigurationsAsync<T>(Func<IWorkflowBlueprint, bool>? predicate, IServiceProvider serviceProvider, [EnumeratorCancellation] CancellationToken cancellationToken) where T : IRabbitMqActivity
        {
            var workflowRegistry = serviceProvider.GetRequiredService<IWorkflowRegistry>();
            var workflowBlueprintReflector = serviceProvider.GetRequiredService<IWorkflowBlueprintReflector>();
            var workflows = await workflowRegistry.ListActiveAsync(cancellationToken);

            var query =
                from workflow in workflows
                from activity in workflow.Activities
                where activity.Type == typeof(T).Name
                select workflow;

            var filteredQuery = predicate == null ? query : query.Where(predicate);

            foreach (var workflow in filteredQuery)
            {
                var workflowBlueprintWrapper = await workflowBlueprintReflector.ReflectAsync(serviceProvider, workflow, cancellationToken);

                foreach (var activity in workflowBlueprintWrapper.Filter<T>())
                {
                    var connectionString = await activity.EvaluatePropertyValueAsync(x => x.ConnectionString, cancellationToken);
                    var routingKey = await activity.EvaluatePropertyValueAsync(x => x.RoutingKey, cancellationToken);
                    var exchangeName = await activity.EvaluatePropertyValueAsync(x => x.ExchangeName, cancellationToken);
                    var headers = await activity.EvaluatePropertyValueAsync(x => x.Headers, cancellationToken);

                    var config = new RabbitMqBusConfiguration(connectionString!, exchangeName!, routingKey!, headers!);

                    yield return config!;
                }
            }
        }

        public async Task<Worker> CreateWorkerAsync(IServiceProvider serviceProvider, RabbitMqBusConfiguration config, CancellationToken cancellationToken = default)
        {
            var receiver = await _messageReceiverClientFactory.GetReceiverAsync(config, cancellationToken);
            return ActivatorUtilities.CreateInstance<Worker>(serviceProvider, (Func<IClient, Task>)DisposeWorkerAsync, receiver);
        }

        private async Task DisposeWorkerAsync(IClient messageReceiver) => await _messageReceiverClientFactory.DisposeReceiverAsync(messageReceiver);

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