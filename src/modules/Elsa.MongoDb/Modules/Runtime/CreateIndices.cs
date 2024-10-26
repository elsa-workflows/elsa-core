using Elsa.KeyValues.Entities;
using Elsa.MongoDb.Helpers;
using Elsa.Workflows.Runtime.Entities;
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
            CreateWorkflowExecutionLogIndices(scope, cancellationToken),
            CreateActivityExecutionLogIndices(scope, cancellationToken),
            CreateBookmarkIndices(scope, cancellationToken),
            CreateBookmarkQueueIndices(scope, cancellationToken),
            CreateWorkflowTriggerIndices(scope, cancellationToken),
            CreateKeyValueIndices(scope, cancellationToken)
        );
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
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
                        new(indexBuilder.Ascending(x => new
                        {
                            x.Id,
                            x.TenantId
                        }), new CreateIndexOptions
                        {
                            Unique = true
                        }),
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
                        new(indexBuilder.Ascending(x => x.WorkflowVersion)),
                        new(indexBuilder.Ascending(x => x.TenantId))
                    },
                    cancellationToken));
    }

    private static Task CreateActivityExecutionLogIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var activityExecutionLogCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<ActivityExecutionRecord>>();
        if (activityExecutionLogCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            activityExecutionLogCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<ActivityExecutionRecord>>
                    {
                        new(indexBuilder.Ascending(x => new
                        {
                            x.Id,
                            x.TenantId
                        }), new CreateIndexOptions
                        {
                            Unique = true
                        }),
                        new(indexBuilder.Ascending(x => x.StartedAt)),
                        new(indexBuilder.Ascending(x => x.ActivityId)),
                        new(indexBuilder.Ascending(x => x.ActivityType)),
                        new(indexBuilder.Ascending(x => x.ActivityTypeVersion)),
                        new(indexBuilder.Ascending(x => x.ActivityName)),
                        new(indexBuilder.Ascending(x => x.WorkflowInstanceId)),
                        new(indexBuilder.Ascending(x => x.HasBookmarks)),
                        new(indexBuilder.Ascending(x => x.Status)),
                        new(indexBuilder.Ascending(x => x.CompletedAt)),
                        new(indexBuilder.Ascending(x => x.TenantId))
                    },
                    cancellationToken));
    }

    private static Task CreateBookmarkIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var bookmarkCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<StoredBookmark>>();
        if (bookmarkCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            bookmarkCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<StoredBookmark>>
                    {
                        new(indexBuilder.Ascending(x => new
                        {
                            x.Id,
                            x.TenantId
                        }), new CreateIndexOptions
                        {
                            Unique = true
                        }),
                        new(indexBuilder
                            .Ascending(x => x.ActivityTypeName)
                            .Ascending(x => x.Hash)),
                        new(indexBuilder
                            .Ascending(x => x.ActivityTypeName)
                            .Ascending(x => x.Hash)
                            .Ascending(x => x.WorkflowInstanceId)),
                        new(indexBuilder.Ascending(x => x.WorkflowInstanceId)),
                        new(indexBuilder.Ascending(x => x.ActivityTypeName)),
                        new(indexBuilder.Ascending(x => x.Hash)),
                        new(indexBuilder.Ascending(x => x.TenantId))
                    },
                    cancellationToken));
    }

    private static Task CreateBookmarkQueueIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var bookmarkQueueCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<BookmarkQueueItem>>();
        if (bookmarkQueueCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            bookmarkQueueCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<BookmarkQueueItem>>
                    {
                        new(indexBuilder.Ascending(x => new
                        {
                            x.Id,
                            x.TenantId
                        }), new CreateIndexOptions
                        {
                            Unique = true
                        }),
                        new(indexBuilder.Ascending(x => x.WorkflowInstanceId)),
                        new(indexBuilder.Ascending(x => x.CorrelationId)),
                        new(indexBuilder.Ascending(x => x.ActivityTypeName)),
                        new(indexBuilder.Ascending(x => x.ActivityInstanceId)),
                        new(indexBuilder.Ascending(x => x.BookmarkId)),
                        new(indexBuilder.Ascending(x => x.StimulusHash)),
                        new(indexBuilder.Ascending(x => x.TenantId)),
                        new(indexBuilder.Ascending(x => x.CreatedAt))
                    },
                    cancellationToken));
    }

    private static Task CreateWorkflowTriggerIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var workflowTriggerCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<StoredTrigger>>();
        if (workflowTriggerCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            workflowTriggerCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<StoredTrigger>>
                    {
                        new(indexBuilder.Ascending(x => new
                        {
                            x.Id,
                            x.TenantId
                        }), new CreateIndexOptions
                        {
                            Unique = true
                        }),
                        new(indexBuilder.Ascending(x => x.WorkflowDefinitionId)),
                        new(indexBuilder.Ascending(x => x.WorkflowDefinitionVersionId)),
                        new(indexBuilder.Ascending(x => x.Name)),
                        new(indexBuilder.Ascending(x => x.Hash)),
                        new(indexBuilder.Ascending(x => x.TenantId))
                    },
                    cancellationToken));
    }

    private Task CreateKeyValueIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var keyValuePairCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<SerializedKeyValuePair>>();
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