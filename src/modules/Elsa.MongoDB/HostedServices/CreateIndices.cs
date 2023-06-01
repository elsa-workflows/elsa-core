using Elsa.Identity.Entities;
using Elsa.Labels.Entities;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using StoredBookmark = Elsa.Workflows.Runtime.Entities.StoredBookmark;
using WorkflowExecutionLogRecord = Elsa.Workflows.Runtime.Entities.WorkflowExecutionLogRecord;

namespace Elsa.MongoDB.HostedServices;

internal class CreateIndices : IHostedService
{
    private readonly IMongoCollection<Application> _applicationCollection;
    private readonly IMongoCollection<User> _userCollection;
    private readonly IMongoCollection<Role> _roleCollection;
    private readonly IMongoCollection<WorkflowDefinitionLabel> _workflowDefinitionLabelCollection;
    private readonly IMongoCollection<WorkflowDefinition> _workflowDefinitionCollection;
    private readonly IMongoCollection<WorkflowInstance> _workflowInstanceCollection;
    private readonly IMongoCollection<WorkflowState> _workflowStateCollection;
    private readonly IMongoCollection<WorkflowExecutionLogRecord> _workflowExecutionLogCollection;
    private readonly IMongoCollection<StoredBookmark> _workflowBookmarkCollection;
    private readonly IMongoCollection<StoredTrigger> _workflowTriggerCollection;

    public CreateIndices(
        IMongoCollection<Application> applicationCollection,
        IMongoCollection<User> userCollection,
        IMongoCollection<Role> roleCollection,
        IMongoCollection<WorkflowDefinitionLabel> workflowDefinitionLabelCollection,
        IMongoCollection<WorkflowDefinition> workflowDefinitionCollection,
        IMongoCollection<WorkflowInstance> workflowInstanceCollection,
        IMongoCollection<WorkflowState> workflowStateCollection,
        IMongoCollection<WorkflowExecutionLogRecord> workflowExecutionLogCollection,
        IMongoCollection<StoredBookmark> workflowBookmarkCollection,
        IMongoCollection<StoredTrigger> workflowTriggerCollection)
    {
        _applicationCollection = applicationCollection;
        _userCollection = userCollection;
        _roleCollection = roleCollection;
        _workflowDefinitionLabelCollection = workflowDefinitionLabelCollection;
        _workflowDefinitionCollection = workflowDefinitionCollection;
        _workflowInstanceCollection = workflowInstanceCollection;
        _workflowStateCollection = workflowStateCollection;
        _workflowExecutionLogCollection = workflowExecutionLogCollection;
        _workflowBookmarkCollection = workflowBookmarkCollection;
        _workflowTriggerCollection = workflowTriggerCollection;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.WhenAll(
            CreateApplicationIndices(cancellationToken),
            CreateUserIndices(cancellationToken),
            CreateRoleIndices(cancellationToken),
            CreateWorkflowDefinitionLabelIndices(cancellationToken),
            CreateWorkflowDefinitionIndices(cancellationToken),
            CreateWorkflowInstanceIndices(cancellationToken),
            CreateWorkflowStateIndices(cancellationToken),
            CreateWorkflowExecutionLogIndices(cancellationToken),
            CreateWorkflowBookmarkIndices(cancellationToken),
            CreateWorkflowTriggerIndices(cancellationToken));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private Task CreateApplicationIndices(CancellationToken cancellationToken)
    {
        return CreateAsync(
            _applicationCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<Application>>
                    {
                        new(indexBuilder.Ascending(x => x.ClientId), 
                            new CreateIndexOptions {Unique = true}),
                        new(indexBuilder.Ascending(x => x.Name), 
                            new CreateIndexOptions {Unique = true})
                    },
                    cancellationToken));
    }
    
    private Task CreateUserIndices(CancellationToken cancellationToken)
    {
        return CreateAsync(
            _userCollection,
            async (collection, indexBuilder) =>
            {
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<User>>
                    {
                        new(indexBuilder.Ascending(x => x.Name), 
                            new CreateIndexOptions {Unique = true})
                    },
                    cancellationToken);
            });
    }

    private Task CreateRoleIndices(CancellationToken cancellationToken)
    {
        return CreateAsync(
            _roleCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<Role>>
                    {
                        new(indexBuilder.Ascending(x => x.Name), 
                            new CreateIndexOptions {Unique = true})
                    },
                    cancellationToken));
    }
    
    private Task CreateWorkflowDefinitionLabelIndices(CancellationToken cancellationToken)
    {
        return CreateAsync(
            _workflowDefinitionLabelCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<WorkflowDefinitionLabel>>
                    {
                        new(indexBuilder.Ascending(x => x.WorkflowDefinitionId)),
                        new(indexBuilder.Ascending(x => x.WorkflowDefinitionVersionId))
                    },
                    cancellationToken));
    }
    
    private Task CreateWorkflowDefinitionIndices(CancellationToken cancellationToken)
    {
        return CreateAsync(
            _workflowDefinitionCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<WorkflowDefinition>>
                    {
                        new(indexBuilder.Ascending(x => x.DefinitionId).Ascending(x => x.Version), 
                            new CreateIndexOptions {Unique = true}),
                        new(indexBuilder.Ascending(x => x.Version)),
                        new(indexBuilder.Ascending(x => x.Name)),
                        new(indexBuilder.Ascending(x => x.IsLatest)),
                        new(indexBuilder.Ascending(x => x.IsPublished))
                    },
                    cancellationToken));
    }
    
    private Task CreateWorkflowInstanceIndices(CancellationToken cancellationToken)
    {
        return CreateAsync(
            _workflowInstanceCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<WorkflowInstance>>
                    {
                        new(indexBuilder
                            .Ascending(x => x.DefinitionId)
                            .Ascending(x => x.Version)
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
                        new(indexBuilder.Ascending(x => x.Status)),
                        new(indexBuilder.Ascending(x => x.SubStatus)),
                        new(indexBuilder.Ascending(x => x.CorrelationId)),
                        new(indexBuilder.Ascending(x => x.Name)),
                        new(indexBuilder.Ascending(x => x.CreatedAt)),
                        new(indexBuilder.Ascending(x => x.LastExecutedAt)),
                        new(indexBuilder.Ascending(x => x.FinishedAt)),
                        new(indexBuilder.Ascending(x => x.FaultedAt)),
                    },
                    cancellationToken));
    }
    
    private Task CreateWorkflowStateIndices(CancellationToken cancellationToken)
    {
        return CreateAsync(
            _workflowStateCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<WorkflowState>>
                    {
                        new(indexBuilder
                            .Ascending(x => x.DefinitionId)
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
        return CreateAsync(
            _workflowExecutionLogCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<WorkflowExecutionLogRecord>>
                    {
                        new(indexBuilder.Ascending(x => x.WorkflowDefinitionId)),
                        new(indexBuilder.Ascending(x => x.Timestamp)),
                        new(indexBuilder.Ascending(x => x.ActivityInstanceId)),
                        new(indexBuilder.Ascending(x => x.ParentActivityInstanceId)),
                        new(indexBuilder.Ascending(x => x.ActivityId)),
                        new(indexBuilder.Ascending(x => x.ActivityType)),
                        new(indexBuilder.Ascending(x => x.EventName)),
                        new(indexBuilder.Ascending(x => x.WorkflowInstanceId)),
                        new(indexBuilder.Ascending(x => x.WorkflowVersion))
                    },
                    cancellationToken));
    }
    
    private Task CreateWorkflowBookmarkIndices(CancellationToken cancellationToken)
    {
        return CreateAsync(
            _workflowBookmarkCollection,
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
                        new(indexBuilder.Ascending(x => x.Hash)),
                        new(indexBuilder.Ascending(x => x.BookmarkId), new CreateIndexOptions { Unique = true })
                    },
                    cancellationToken));
    }
    
    private Task CreateWorkflowTriggerIndices(CancellationToken cancellationToken)
    {
        return CreateAsync(
            _workflowTriggerCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<StoredTrigger>>
                    {
                        new(indexBuilder.Ascending(x => x.WorkflowDefinitionId)),
                        new(indexBuilder.Ascending(x => x.Name)),
                        new(indexBuilder.Ascending(x => x.Hash))
                    },
                    cancellationToken));
    }

    private static async Task CreateAsync<T>(
        IMongoCollection<T> collection,
        Func<IMongoCollection<T>, IndexKeysDefinitionBuilder<T>, Task> func)
    {
        var builder = Builders<T>.IndexKeys;
        await func(collection, builder);
    }
}