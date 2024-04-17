using Elsa.Labels.Entities;
using Elsa.MongoDb.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace Elsa.MongoDb.Modules.Labels;

internal class CreateIndices(IServiceProvider serviceProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        return CreateWorkflowDefinitionLabelIndices(scope, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static Task CreateWorkflowDefinitionLabelIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var workflowDefinitionLabelCollection = serviceScope.ServiceProvider.GetService<MongoCollectionBase<WorkflowDefinitionLabel>>();
        if (workflowDefinitionLabelCollection == null) return Task.CompletedTask;
        
        return IndexHelpers.CreateAsync(
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
}