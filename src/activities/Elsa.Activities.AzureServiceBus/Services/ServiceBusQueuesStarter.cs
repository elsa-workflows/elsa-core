using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.AzureServiceBus.Services
{
    // TODO: Look for a way to merge ServiceBusQueuesStarter with ServiceBusTopicsStarter - there's a lot of overlap.
    public class ServiceBusQueuesStarter : IServiceBusQueuesStarter
    {
        private readonly IQueueMessageReceiverClientFactory _messageReceiverClientFactory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly ICollection<QueueWorker> _workers;
        private readonly SemaphoreSlim _semaphore = new(1);

        public ServiceBusQueuesStarter(
            IQueueMessageReceiverClientFactory messageReceiverClientFactory,
            IServiceScopeFactory scopeFactory,
            IServiceProvider serviceProvider,
            ILogger<ServiceBusQueuesStarter> logger)
        {
            _messageReceiverClientFactory = messageReceiverClientFactory;
            _scopeFactory = scopeFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _workers = new List<QueueWorker>();
        }

        public async Task CreateWorkersAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                await DisposeExistingWorkersAsync();
                var queueNames = (await GetQueueNamesAsync(cancellationToken).ToListAsync(cancellationToken)).Distinct();

                foreach (var queueName in queueNames)
                    await CreateAndAddWorkerAsync(queueName, cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task CreateAndAddWorkerAsync(string queueName, CancellationToken cancellationToken)
        {
            try
            {
                var receiver = await _messageReceiverClientFactory.GetReceiverAsync(queueName, cancellationToken);
                var worker = ActivatorUtilities.CreateInstance<QueueWorker>(_serviceProvider, receiver, (Func<IReceiverClient, Task>) DisposeReceiverAsync);
                _workers.Add(worker);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to create a receiver for queue {QueueName}", queueName);
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

        private async Task DisposeReceiverAsync(IReceiverClient messageReceiver) => await _messageReceiverClientFactory.DisposeReceiverAsync(messageReceiver);

        private async IAsyncEnumerable<string> GetQueueNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var workflowRegistry = scope.ServiceProvider.GetRequiredService<IWorkflowRegistry>();
            var workflowBlueprintReflector = scope.ServiceProvider.GetRequiredService<IWorkflowBlueprintReflector>();
            var workflows = await workflowRegistry.ListActiveAsync(cancellationToken);

            var query =
                from workflow in workflows
                from activity in workflow.Activities
                where activity.Type == nameof(AzureServiceBusQueueMessageReceived)
                select workflow;

            foreach (var workflow in query)
            {
                var workflowBlueprintWrapper = await workflowBlueprintReflector.ReflectAsync(scope.ServiceProvider, workflow, cancellationToken);

                foreach (var activity in workflowBlueprintWrapper.Filter<AzureServiceBusQueueMessageReceived>())
                {
                    var queueName = await activity.EvaluatePropertyValueAsync(x => x.QueueName, cancellationToken);

                    if (string.IsNullOrWhiteSpace(queueName))
                    {
                        _logger.LogWarning(
                            "Encountered a queue name that is null or empty in activity {ActivityType}:{ActivityId} in workflow {WorkflowDefinitionId}:v{WorkflowDefinitionVersion}",
                            activity.ActivityBlueprint.Type,
                            activity.ActivityBlueprint.Id,
                            workflow.Id,
                            workflow.Version);

                        continue;
                    }

                    yield return queueName!;
                }
            }
        }
    }
}