using Elsa.Extensions;
using Elsa.Kafka.Activities;
using Elsa.Kafka.Stimuli;
using Elsa.Workflows;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.DependencyInjection;
using Open.Linq.AsyncExtensions;

namespace Elsa.Kafka.Implementations;

public class WorkerManager(IHasher hasher, IServiceScopeFactory scopeFactory) : IWorkerManager
{
    private static readonly string MessageReceivedActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<MessageReceived>();
    private IDictionary<string, IWorker> Workers { get; set; } = new Dictionary<string, IWorker>();

    public async Task UpdateWorkersAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var consumerDefinitionEnumerator = scope.ServiceProvider.GetRequiredService<IConsumerDefinitionEnumerator>();
        var consumerDefinitions = await consumerDefinitionEnumerator.EnumerateAsync(cancellationToken).ToList();
        var workers = new Dictionary<string, IWorker>(Workers);
        var workersToRemove = workers.Keys.Except(consumerDefinitions.Select(x => x.Id)).ToList();

        // Remove workers that are no longer needed.
        foreach (var workerToRemove in workersToRemove)
        {
            if (workers.TryGetValue(workerToRemove, out var worker))
            {
                worker.Stop();
                workers.Remove(workerToRemove);
            }
        }

        // Add or update workers.
        foreach (var consumerDefinition in consumerDefinitions)
        {
            var existingWorker = workers.GetValueOrDefault(consumerDefinition.Id);

            if (existingWorker == null)
            {
                // Add a new worker.
                var worker = await CreateWorkerAsync(scope.ServiceProvider, consumerDefinition, cancellationToken);
                workers.Add(consumerDefinition.Id, worker);
            }
            else
            {
                // Compare the existing worker's consumer definition with the new consumer definition using a hash.
                var existingHash = ComputeHash(existingWorker.ConsumerDefinition);
                var hash = ComputeHash(consumerDefinition);

                // If the hash is different, update the worker.
                if (existingHash != hash)
                {
                    existingWorker.Stop();
                    var worker = await CreateWorkerAsync(scope.ServiceProvider, consumerDefinition, cancellationToken);
                    workers[consumerDefinition.Id] = worker;
                }
            }
        }

        // Ensure all workers are running.
        foreach (var worker in workers.Values)
            worker.Start(cancellationToken);

        // Update the worker dictionary.
        Workers = workers;
    }

    public async Task BindTriggersAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default)
    {
        var triggerList = triggers.Where(x => x.Name == MessageReceivedActivityTypeName).ToList();
        await using var scope = scopeFactory.CreateAsyncScope();

        // Bind triggers to workers.
        var workflowDefinitionService = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionService>();
        foreach (var trigger in triggerList)
        {
            var workflow = await workflowDefinitionService.FindWorkflowAsync(trigger.WorkflowDefinitionVersionId, cancellationToken);

            if (workflow == null)
                continue;

            var stimulus = trigger.GetPayload<MessageReceivedStimulus>();
            var consumerDefinitionId = stimulus.ConsumerDefinitionId;
            var worker = GetWorker(consumerDefinitionId);

            if (worker == null)
                continue;

            var triggerBinding = new TriggerBinding(workflow, trigger.Id, trigger.ActivityId, stimulus);
            worker.BindTrigger(triggerBinding);
        }
    }

    public Task UnbindTriggersAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default)
    {
        var triggerList = triggers.Where(x => x.Name == MessageReceivedActivityTypeName).ToList();
        var removedTriggerIds = triggerList.Select(x => x.Id).ToList();

        foreach (var trigger in triggerList)
        {
            var consumerDefinitionId = trigger.GetPayload<MessageReceivedStimulus>().ConsumerDefinitionId;
            var worker = GetWorker(consumerDefinitionId);

            worker?.RemoveTriggers(removedTriggerIds);
        }
        
        return Task.CompletedTask;
    }

    public Task BindBookmarksAsync(IEnumerable<StoredBookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var bookmarkList = bookmarks.Where(x => x.Name == MessageReceivedActivityTypeName).ToList();

        if (bookmarkList.Count == 0)
            return Task.CompletedTask;

        // Bind bookmarks to workers.
        foreach (var bookmark in bookmarkList)
        {
            var stimulus = bookmark.GetPayload<MessageReceivedStimulus>();
            var consumerDefinitionId = stimulus.ConsumerDefinitionId;
            var worker = GetWorker(consumerDefinitionId);

            if (worker == null)
                continue;

            var bookmarkBinding = new BookmarkBinding(bookmark.WorkflowInstanceId, bookmark.CorrelationId, bookmark.Id, stimulus);
            worker.BindBookmark(bookmarkBinding);
        }

        return Task.CompletedTask;
    }
    
    public Task UnbindBookmarksAsync(IEnumerable<StoredBookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var bookmarkList = bookmarks.Where(x => x.Name == MessageReceivedActivityTypeName).ToList();
        var removedBookmarkIds = bookmarkList.Select(x => x.Id).ToList();

        foreach (var bookmark in bookmarkList)
        {
            var stimulus = bookmark.GetPayload<MessageReceivedStimulus>();
            var consumerDefinitionId = stimulus.ConsumerDefinitionId;
            var worker = GetWorker(consumerDefinitionId);

            worker?.RemoveBookmarks(removedBookmarkIds);
        }
        
        return Task.CompletedTask;
    }

    public void StopWorkers()
    {
        foreach (var worker in Workers.Values)
            worker.Stop();
    }

    public IWorker? GetWorker(string consumerDefinitionId)
    {
        return Workers.TryGetValue(consumerDefinitionId, out var worker) ? worker : null;
    }

    private async Task<IWorker> CreateWorkerAsync(IServiceProvider serviceProvider, ConsumerDefinition consumerDefinition, CancellationToken cancellationToken)
    {
        var factoryType = consumerDefinition.FactoryType;
        
        if(factoryType == null!)
            throw new InvalidOperationException("Worker factory type not specified.");
        
        var consumerFactory = ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, factoryType) as IConsumerFactory;
        
        if (consumerFactory == null)
            throw new InvalidOperationException($"Worker factory of type '{factoryType}' not found.");

        var schemaRegistryDefinition = await GetSchemaRegistryDefinitionAsync(serviceProvider, consumerDefinition.SchemaRegistryId, cancellationToken);
        var createConsumerContext = new CreateConsumerContext(consumerDefinition, schemaRegistryDefinition);
        var consumerProxy = consumerFactory.CreateConsumer(createConsumerContext);
        var wrappedConsumer = consumerProxy.Consumer;
        var wrappedConsumerType = wrappedConsumer.GetType();
        var keyType = wrappedConsumerType.GetGenericArguments()[0];
        var valueType = wrappedConsumerType.GetGenericArguments()[1];
        var workerType = typeof(Worker<,>).MakeGenericType(keyType, valueType);
        var workerContext = new WorkerContext(serviceProvider.GetRequiredService<IServiceScopeFactory>(), consumerDefinition);
        var worker = (IWorker)ActivatorUtilities.CreateInstance(serviceProvider, workerType, workerContext, consumerProxy.Consumer);
        return worker;
    }

    private string ComputeHash(ConsumerDefinition consumerDefinition)
    {
        return hasher.Hash(consumerDefinition);
    }
    
    private async Task<SchemaRegistryDefinition?> GetSchemaRegistryDefinitionAsync(IServiceProvider serviceProvider, string? id, CancellationToken cancellationToken = default)
    {
        if (id == null)
            return null;
        
        var schemaRegistryDefinitionEnumerator = serviceProvider.GetRequiredService<ISchemaRegistryDefinitionEnumerator>();
        var schemaRegistryDefinitions = await schemaRegistryDefinitionEnumerator.EnumerateAsync(cancellationToken).ToList();
        return schemaRegistryDefinitions.FirstOrDefault(x => x.Id == id);
    }
}