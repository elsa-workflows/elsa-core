using Elsa.MongoDb.Helpers;
using Elsa.Workflows.Management.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace Elsa.MongoDb.Modules.Management;

internal class CreateIndices(IServiceProvider serviceProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        return Task.WhenAll(
            CreateWorkflowDefinitionIndices(scope, cancellationToken),
            CreateWorkflowInstanceIndices(scope, cancellationToken));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static Task CreateWorkflowDefinitionIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var workflowDefinitionCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<WorkflowDefinition>>();
        if (workflowDefinitionCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            workflowDefinitionCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<WorkflowDefinition>>
                    {
                        new(indexBuilder.Ascending(x => new
                        {
                            x.Id,
                            x.TenantId
                        }), new CreateIndexOptions
                        {
                            Unique = true
                        }),
                        new(indexBuilder.Ascending(x => x.DefinitionId).Ascending(x => x.Version), new CreateIndexOptions
                        {
                            Unique = true
                        }),
                        new(indexBuilder.Ascending(x => x.Version)),
                        new(indexBuilder.Ascending(x => x.Name)),
                        new(indexBuilder.Ascending(x => x.IsSystem)),
                        new(indexBuilder.Ascending(x => x.IsLatest)),
                        new(indexBuilder.Ascending(x => x.IsPublished)),
                        new(indexBuilder.Ascending(x => x.TenantId))
                    },
                    cancellationToken));
    }

    private static Task CreateWorkflowInstanceIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var workflowInstanceCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<WorkflowInstance>>();
        if (workflowInstanceCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            workflowInstanceCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<WorkflowInstance>>
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
                        new(indexBuilder.Ascending(x => x.IsSystem)),
                        new(indexBuilder.Ascending(x => x.CreatedAt)),
                        new(indexBuilder.Ascending(x => x.UpdatedAt)),
                        new(indexBuilder.Ascending(x => x.FinishedAt)),
                        new(indexBuilder.Ascending(x => x.TenantId))
                    },
                    cancellationToken));
    }
}

