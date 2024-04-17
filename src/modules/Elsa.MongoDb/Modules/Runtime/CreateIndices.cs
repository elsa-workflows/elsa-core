using Elsa.KeyValues.Entities;
using Elsa.MongoDb.Helpers;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using StoredBookmark = Elsa.Workflows.Runtime.Entities.StoredBookmark;
using WorkflowExecutionLogRecord = Elsa.Workflows.Runtime.Entities.WorkflowExecutionLogRecord;

namespace Elsa.MongoDb.Modules.Runtime;

internal class CreateIndices(IServiceProvider serviceProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        return Task.WhenAll(
            CreateWorkflowStateIndices(scope, cancellationToken),
            CreateWorkflowExecutionLogIndices(scope, cancellationToken),
            CreateActivityExecutionLogIndices(scope, cancellationToken),
            CreateWorkflowBookmarkIndices(scope, cancellationToken),
            CreateWorkflowTriggerIndices(scope, cancellationToken),
            CreateWorkflowInboxIndices(scope, cancellationToken),
            CreateKeyValueIndices(scope, cancellationToken)
        );
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static Task CreateWorkflowStateIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var workflowStateCollection = serviceScope.ServiceProvider.GetService<MongoCollectionBase<WorkflowState>>();
        if (workflowStateCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            workflowStateCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<WorkflowState>>
                    {
                        new(indexBuilder
                            .Ascending(x => x.DefinitionId)
                            .Ascending(x => x.DefinitionVersionId)
                            .Ascending(x => x.DefinitionVersion)
                            .Ascending(x => x.SubStatus)
                            .Ascending(x => x.Status)),
                        new(indexBuilder
                            .Ascending(x => x.SubStatus)
                            .Ascending(x => x.Status)),
                        new(indexBuilder
                            .Ascending(x => x.DefinitionId)
                            .Ascending(x => x.Status)),
                        new(indexBuilder
                            .Ascending(x => x.DefinitionId)
                            .Ascending(x => x.SubStatus)),
                        new(indexBuilder.Ascending(x => x.DefinitionId)),
                        new(indexBuilder.Ascending(x => x.CorrelationId)),
                    },
                    cancellationToken));
    }

    private static Task CreateWorkflowExecutionLogIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var workflowExecutionLogCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<WorkflowExecutionLogRecord>>();
        if (workflowExecutionLogCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            workflowExecutionLogCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<WorkflowExecutionLogRecord>>
                    {
                        new(indexBuilder.Ascending(x => x.WorkflowDefinitionId)),
                        new(indexBuilder.Ascending(x => x.WorkflowDefinitionVersionId)),
                        new(indexBuilder.Ascending(x => x.Sequence)),
                        new(indexBuilder.Ascending(x => x.Timestamp)),
                        new(indexBuilder.Ascending(x => x.Timestamp).Ascending(x => x.Sequence)),
                        new(indexBuilder.Ascending(x => x.ActivityInstanceId)),
                        new(indexBuilder.Ascending(x => x.ParentActivityInstanceId)),
                        new(indexBuilder.Ascending(x => x.ActivityId)),
                        new(indexBuilder.Ascending(x => x.ActivityType)),
                        new(indexBuilder.Ascending(x => x.ActivityTypeVersion)),
                        new(indexBuilder.Ascending(x => x.ActivityName)),
                        new(indexBuilder.Ascending(x => x.EventName)),
                        new(indexBuilder.Ascending(x => x.WorkflowInstanceId)),
                        new(indexBuilder.Ascending(x => x.WorkflowVersion))
                    },
                    cancellationToken));
    }

    private static Task CreateActivityExecutionLogIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var activityExecutionLogCollection = serviceScope.ServiceProvider.GetService<MongoCollectionBase<ActivityExecutionRecord>>();
        if (activityExecutionLogCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            activityExecutionLogCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<ActivityExecutionRecord>>
                    {
                        new(indexBuilder.Ascending(x => x.StartedAt)),
                        new(indexBuilder.Ascending(x => x.ActivityId)),
                        new(indexBuilder.Ascending(x => x.ActivityType)),
                        new(indexBuilder.Ascending(x => x.ActivityTypeVersion)),
                        new(indexBuilder.Ascending(x => x.ActivityName)),
                        new(indexBuilder.Ascending(x => x.WorkflowInstanceId)),
                        new(indexBuilder.Ascending(x => x.HasBookmarks)),
                        new(indexBuilder.Ascending(x => x.Status)),
                        new(indexBuilder.Ascending(x => x.CompletedAt))
                    },
                    cancellationToken));
    }

    private static Task CreateWorkflowBookmarkIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var workflowBookmarkCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<StoredBookmark>>();
        if (workflowBookmarkCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            workflowBookmarkCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<StoredBookmark>>
                    {
                        new(indexBuilder
                            .Ascending(x => x.ActivityTypeName)
                            .Ascending(x => x.Hash)),
                        new(indexBuilder
                            .Ascending(x => x.ActivityTypeName)
                            .Ascending(x => x.Hash)
                            .Ascending(x => x.WorkflowInstanceId)),
                        new(indexBuilder.Ascending(x => x.WorkflowInstanceId)),
                        new(indexBuilder.Ascending(x => x.ActivityTypeName)),
                        new(indexBuilder.Ascending(x => x.Hash))
                    },
                    cancellationToken));
    }

    private static Task CreateWorkflowTriggerIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var workflowTriggerCollection = serviceScope.ServiceProvider.GetService<MongoCollectionBase<StoredTrigger>>();
        if (workflowTriggerCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            workflowTriggerCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<StoredTrigger>>
                    {
                        new(indexBuilder.Ascending(x => x.WorkflowDefinitionId)),
                        new(indexBuilder.Ascending(x => x.WorkflowDefinitionVersionId)),
                        new(indexBuilder.Ascending(x => x.Name)),
                        new(indexBuilder.Ascending(x => x.Hash))
                    },
                    cancellationToken));
    }

    private static Task CreateWorkflowInboxIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var collection = serviceScope.ServiceProvider.GetService<MongoCollectionBase<WorkflowInboxMessage>>();
        if (collection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            collection,
            async (col, indexBuilder) =>
                await col.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<WorkflowInboxMessage>>
                    {
                        new(indexBuilder.Ascending(x => x.ActivityTypeName)),
                        new(indexBuilder.Ascending(x => x.Hash)),
                        new(indexBuilder.Ascending(x => x.WorkflowInstanceId)),
                        new(indexBuilder.Ascending(x => x.CorrelationId)),
                        new(indexBuilder.Ascending(x => x.CreatedAt)),
                        new(indexBuilder.Ascending(x => x.ExpiresAt)),
                    },
                    cancellationToken));
    }

    private Task CreateKeyValueIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var keyValuePairCollection = serviceScope.ServiceProvider.GetService<MongoCollectionBase<SerializedKeyValuePair>>();
        if (keyValuePairCollection == null) return Task.CompletedTask;
        
        return IndexHelpers.CreateAsync(
            keyValuePairCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<SerializedKeyValuePair>>
                    {
                        new(indexBuilder.Ascending(x => x.Key))
                    },
                    cancellationToken));
    }
}