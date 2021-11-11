using Elsa.Activities.RabbitMq;
using Elsa.Activities.RabbitMq.Configuration;
using Elsa.Activities.RabbitMq.Services;
using Elsa.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Elsa.WorkflowTesting.Services
{
    public class RabbitMqTestQueueManager : IRabbitMqTestQueueManager
    {
        private readonly IConfiguration _configuration;
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDictionary<string, ICollection<WorkerBase>> _workers;
        private readonly IRabbitMqQueueStarter _rabbitMqQueueStarter;
        private readonly ILogger _logger;

        public RabbitMqTestQueueManager(
            IConfiguration configuration, 
            IServiceScopeFactory scopeFactory, 
            IServiceProvider serviceProvider,
            IRabbitMqQueueStarter rabbitMqQueueStarter,
            ILogger<RabbitMqQueueStarter> logger)
        {
            _configuration = configuration;
            _scopeFactory = scopeFactory;
            _serviceProvider = serviceProvider;
            _rabbitMqQueueStarter = rabbitMqQueueStarter;
            _logger = logger;
            _workers = new Dictionary<string, ICollection<WorkerBase>>();
        }

        public async Task CreateTestWorkersAsync(string workflowId, string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                if (_workers.ContainsKey(workflowInstanceId))
                {
                    if (_workers[workflowInstanceId].Count > 0)
                        return;
                }
                else
                    _workers[workflowInstanceId] = new List<WorkerBase>();

                var receiverConfigs = (await GetReceiverConfigurationsAsync(workflowId, cancellationToken).ToListAsync(cancellationToken)).Distinct();
                var senderConfigs = (await GetSenderConfigurationsAsync(workflowId, cancellationToken).ToListAsync(cancellationToken)).Distinct();

                foreach (var config in receiverConfigs)
                    _workers[workflowInstanceId].Add(await _rabbitMqQueueStarter.CreateReceiverWorkerAsync(config, cancellationToken));

                foreach (var config in senderConfigs)
                    _workers[workflowInstanceId].Add(await _rabbitMqQueueStarter.CreateSenderWorkerAsync(config, cancellationToken));
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async IAsyncEnumerable<RabbitMqBusConfiguration> GetReceiverConfigurationsAsync(string workflowId, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var workflowRegistry = scope.ServiceProvider.GetRequiredService<IWorkflowRegistry>();
            var workflowBlueprintReflector = scope.ServiceProvider.GetRequiredService<IWorkflowBlueprintReflector>();
            var workflows = await workflowRegistry.ListActiveAsync(cancellationToken);

            var query =
                from workflow in workflows
                from activity in workflow.Activities
                where activity.Type == nameof(RabbitMqMessageReceived) && workflow.Id == workflowId
                select workflow;

            foreach (var workflow in query)
            {
                var workflowBlueprintWrapper = await workflowBlueprintReflector.ReflectAsync(scope.ServiceProvider, workflow, cancellationToken);

                foreach (var activity in workflowBlueprintWrapper.Filter<RabbitMqMessageReceived>())
                {
                    var connectionString = await activity.EvaluatePropertyValueAsync(x => x.ConnectionString, cancellationToken);
                    var routingKey = await activity.EvaluatePropertyValueAsync(x => x.RoutingKey, cancellationToken);
                    var headers = await activity.EvaluatePropertyValueAsync(x => x.Headers, cancellationToken);

                    var config = new RabbitMqBusConfiguration(connectionString, routingKey, headers);

                    yield return config!;
                }
            }
        }

        private async IAsyncEnumerable<RabbitMqBusConfiguration> GetSenderConfigurationsAsync(string workflowId, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var workflowRegistry = scope.ServiceProvider.GetRequiredService<IWorkflowRegistry>();
            var workflowBlueprintReflector = scope.ServiceProvider.GetRequiredService<IWorkflowBlueprintReflector>();
            var workflows = await workflowRegistry.ListActiveAsync(cancellationToken);

            var query =
                from workflow in workflows
                from activity in workflow.Activities
                where activity.Type == nameof(SendRabbitMqMessage) && workflow.Id == workflowId
                select workflow;

            foreach (var workflow in query)
            {
                var workflowBlueprintWrapper = await workflowBlueprintReflector.ReflectAsync(scope.ServiceProvider, workflow, cancellationToken);

                foreach (var activity in workflowBlueprintWrapper.Filter<SendRabbitMqMessage>())
                {
                    var connectionString = await activity.EvaluatePropertyValueAsync(x => x.ConnectionString, cancellationToken);
                    var routingKey = await activity.EvaluatePropertyValueAsync(x => x.Topic, cancellationToken);
                    var headers = await activity.EvaluatePropertyValueAsync(x => x.Headers, cancellationToken);

                    var config = new RabbitMqBusConfiguration(connectionString, routingKey, headers);

                    yield return config!;
                }
            }
        }

        public async Task DisposeTestWorkersAsync(string workflowInstance)
        {
            foreach (var worker in _workers[workflowInstance])
            {
                await worker.DisposeAsync();
            }

            _workers[workflowInstance].Clear();
        }
    }
}