using Elsa.MongoDb.Helpers;
using Elsa.Workflows.Management.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace Elsa.MongoDb.Modules.Management;

internal class CreateIndices : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public CreateIndices(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.WhenAll(
            CreateWorkflowDefinitionIndices(cancellationToken),
            CreateWorkflowInstanceIndices(cancellationToken));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    private Task CreateWorkflowDefinitionIndices(CancellationToken cancellationToken)
    {
        var workflowDefinitionCollection = _serviceProvider.GetService<IMongoCollection<WorkflowDefinition>>();
        if (workflowDefinitionCollection == null) return Task.CompletedTask;
        
        return IndexHelpers.CreateAsync(
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
        
        return IndexHelpers.CreateAsync(
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
                        new(indexBuilder.Ascending(x => x.UpdatedAt)),
                        new(indexBuilder.Ascending(x => x.FinishedAt))
                    },
                    cancellationToken));
    }
}