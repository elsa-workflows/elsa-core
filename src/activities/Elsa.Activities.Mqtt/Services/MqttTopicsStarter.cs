using Elsa.Activities.Mqtt.Options;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Bookmarks;
using Elsa.Models;
using Elsa.Services.Models;
using Elsa.Services.WorkflowStorage;

namespace Elsa.Activities.Mqtt.Services
{
    public class MqttTopicsStarter : IMqttTopicsStarter
    {
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly IMessageReceiverClientFactory _receiverFactory;
        private readonly IBookmarkSerializer _bookmarkSerializer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MqttTopicsStarter> _logger;
        private readonly IDictionary<int,Worker> _workers;

        public MqttTopicsStarter(
            IMessageReceiverClientFactory receiverFactory,
            IBookmarkSerializer bookmarkSerializer,
            IServiceScopeFactory scopeFactory,
            IServiceProvider serviceProvider,
            ILogger<MqttTopicsStarter> logger)
        {
            _receiverFactory = receiverFactory;
            _bookmarkSerializer = bookmarkSerializer;
            _scopeFactory = scopeFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _workers = new Dictionary<int, Worker>();
        }


        public async Task CreateWorkersAsync(string workflowDefinitionId, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var triggerFinder = scope.ServiceProvider.GetRequiredService<ITriggerFinder>();
            var workflowRegistry = scope.ServiceProvider.GetRequiredService<IWorkflowRegistry>();
            var triggerExtractor = scope.ServiceProvider.GetRequiredService<IGetsTriggersForWorkflowBlueprints>();
            var workflowExecutionContextFactory = scope.ServiceProvider.GetRequiredService<ICreatesWorkflowExecutionContextForWorkflowBlueprint>();
            var workflowBlueprint = workflowRegistry.GetWorkflowAsync(workflowDefinitionId, VersionOptions.All).Result;

            //Worker dispose if workflowDefinitionId not Published.
            if (workflowBlueprint == null || !workflowBlueprint.IsPublished)
            {
                await DisposeExistingWorkersAsync();
            }
            else
            {
                await CreateWorkersAsync(cancellationToken);
            }
        }

        public async Task CreateWorkersAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var receiverConfigs = (await GetConfigurationsAsync(cancellationToken).ToListAsync(cancellationToken)).GroupBy(c => c.GetHashCode()).Select(x => x.First());

                foreach (var config in receiverConfigs)
                {
                    try
                    {
                        if (!_workers.ContainsKey(config.GetHashCode()))
                            _workers.Add(config.GetHashCode(), await CreateWorkerAsync(config, cancellationToken));
                        else
                            _workers[config.GetHashCode()].Ping();
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Failed to create a receiver for routing key {RoutingKey}", "");
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }

        }

        public async Task<Worker> CreateWorkerAsync(MqttClientOptions config, CancellationToken cancellationToken)
        {
            var receiver = await _receiverFactory.GetReceiverAsync(config, cancellationToken);
            return ActivatorUtilities.CreateInstance<Worker>(_serviceProvider, receiver, (Func<IMqttClientWrapper, Task>)DisposeReceiverAsync);
        }

        public async IAsyncEnumerable<MqttClientOptions> GetConfigurationsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var triggerFinder = scope.ServiceProvider.GetRequiredService<ITriggerFinder>();
            var triggers = await triggerFinder.FindTriggersByTypeAsync<MessageReceivedBookmark>(cancellationToken: cancellationToken);

            foreach (var trigger in triggers)
            {
                var bookmarkModel = _bookmarkSerializer.Deserialize<MessageReceivedBookmark>(trigger.Model);

                var configuration = CreateConfigurationFromBookmark(bookmarkModel, trigger.ActivityId);

                yield return configuration;
            }

            var bookmarkFinder = scope.ServiceProvider.GetRequiredService<IBookmarkFinder>();
            var bookmarks = await bookmarkFinder.FindBookmarksByTypeAsync<MessageReceivedBookmark>(cancellationToken: cancellationToken);

            foreach (var bookmark in bookmarks)
            {
                var bookmarkModel = _bookmarkSerializer.Deserialize<MessageReceivedBookmark>(bookmark.Model);

                var configuration = CreateConfigurationFromBookmark(bookmarkModel, bookmark.ActivityId);

                yield return configuration;
            }

        }

        private MqttClientOptions CreateConfigurationFromBookmark(MessageReceivedBookmark bookmark, string activityId)
        {
            return new MqttClientOptions(bookmark.Topic,bookmark.Host,bookmark.Port,bookmark.Username,bookmark.Password,bookmark.Qos);
        }

        private async Task DisposeExistingWorkersAsync()
        {
            foreach (var worker in _workers.ToList())
            {
                await worker.Value.DisposeAsync();
                _workers.Remove(worker);
            }
        }

        private async Task DisposeReceiverAsync(IMqttClientWrapper messageReceiver) => await _receiverFactory.DisposeReceiverAsync(messageReceiver);

        
    }
}