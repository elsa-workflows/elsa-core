using Elsa.MongoDb.Helpers;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using StoredBookmark = Elsa.Workflows.Runtime.Entities.StoredBookmark;
using WorkflowExecutionLogRecord = Elsa.Workflows.Runtime.Entities.WorkflowExecutionLogRecord;

namespace Elsa.MongoDb.Modules.Runtime;

internal class CreateIndices : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public CreateIndices(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.WhenAll(
            CreateWorkflowStateIndices(cancellationToken),
            CreateWorkflowExecutionLogIndices(cancellationToken),
            CreateActivityExecutionLogIndices(cancellationToken),
            CreateWorkflowBookmarkIndices(cancellationToken),
            CreateWorkflowTriggerIndices(cancellationToken),
            CreateWorkflowInboxIndices(cancellationToken)
        );
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private Task CreateWorkflowStateIndices(CancellationToken cancellationToken)
    {
        var workflowStateCollection = _serviceProvider.GetService<IMongoCollection<WorkflowState>>();
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

    private Task CreateWorkflowExecutionLogIndices(CancellationToken cancellationToken)
    {
        var workflowExecutionLogCollection = _serviceProvider.GetService<IMongoCollection<WorkflowExecutionLogRecord>>();
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

    private Task CreateActivityExecutionLogIndices(CancellationToken cancellationToken)
    {
        var activityExecutionLogCollection = _serviceProvider.GetService<IMongoCollection<ActivityExecutionRecord>>();
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

    private Task CreateWorkflowBookmarkIndices(CancellationToken cancellationToken)
    {
        var workflowBookmarkCollection = _serviceProvider.GetService<IMongoCollection<StoredBookmark>>();
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

    private Task CreateWorkflowTriggerIndices(CancellationToken cancellationToken)
    {
        var workflowTriggerCollection = _serviceProvider.GetService<IMongoCollection<StoredTrigger>>();
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

    private Task CreateWorkflowInboxIndices(CancellationToken cancellationToken)
    {
        var collection = _serviceProvider.GetService<IMongoCollection<WorkflowInboxMessage>>();
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
}