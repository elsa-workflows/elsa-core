using Elsa.Identity.Entities;
using Elsa.Labels.Entities;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using StoredBookmark = Elsa.Workflows.Runtime.Entities.StoredBookmark;
using WorkflowExecutionLogRecord = Elsa.Workflows.Runtime.Entities.WorkflowExecutionLogRecord;

namespace Elsa.MongoDb.HostedServices;

internal class CreateIndices : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public CreateIndices(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

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
        var applicationCollection = _serviceProvider.GetService<IMongoCollection<Application>>();
        if (applicationCollection == null) return Task.CompletedTask;
        
        return CreateAsync(
            applicationCollection,
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
        var userCollection = _serviceProvider.GetService<IMongoCollection<User>>();
        if (userCollection == null) return Task.CompletedTask;
        
        return CreateAsync(
            userCollection,
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
        var roleCollection = _serviceProvider.GetService<IMongoCollection<Role>>();
        if (roleCollection == null) return Task.CompletedTask;
        
        return CreateAsync(
            roleCollection,
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
        var workflowDefinitionLabelCollection = _serviceProvider.GetService<IMongoCollection<WorkflowDefinitionLabel>>();
        if (workflowDefinitionLabelCollection == null) return Task.CompletedTask;
        
        return CreateAsync(
            workflowDefinitionLabelCollection,
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
        var workflowDefinitionCollection = _serviceProvider.GetService<IMongoCollection<WorkflowDefinition>>();
        if (workflowDefinitionCollection == null) return Task.CompletedTask;
        
        return CreateAsync(
            workflowDefinitionCollection,
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
        var workflowInstanceCollection = _serviceProvider.GetService<IMongoCollection<WorkflowInstance>>();
        if (workflowInstanceCollection == null) return Task.CompletedTask;
        
        return CreateAsync(
            workflowInstanceCollection,
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
        var workflowStateCollection = _serviceProvider.GetService<IMongoCollection<WorkflowState>>();
        if (workflowStateCollection == null) return Task.CompletedTask;

        return CreateAsync(
            workflowStateCollection,
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
        var workflowExecutionLogCollection = _serviceProvider.GetService<IMongoCollection<WorkflowExecutionLogRecord>>();
        if (workflowExecutionLogCollection == null) return Task.CompletedTask;
        
        return CreateAsync(
            workflowExecutionLogCollection,
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
        var workflowBookmarkCollection = _serviceProvider.GetService<IMongoCollection<StoredBookmark>>();
        if (workflowBookmarkCollection == null) return Task.CompletedTask;
        
        return CreateAsync(
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
                        new(indexBuilder.Ascending(x => x.Hash)),
                        new(indexBuilder.Ascending(x => x.BookmarkId), new CreateIndexOptions { Unique = true })
                    },
                    cancellationToken));
    }
    
    private Task CreateWorkflowTriggerIndices(CancellationToken cancellationToken)
    {
        var workflowTriggerCollection = _serviceProvider.GetService<IMongoCollection<StoredTrigger>>();
        if (workflowTriggerCollection == null) return Task.CompletedTask;
        
        return CreateAsync(
            workflowTriggerCollection,
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