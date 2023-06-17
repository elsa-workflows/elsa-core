using Elsa.Labels.Entities;
using Elsa.MongoDb.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace Elsa.MongoDb.Modules.Labels;

internal class CreateIndices : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public CreateIndices(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return CreateWorkflowDefinitionLabelIndices(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private Task CreateWorkflowDefinitionLabelIndices(CancellationToken cancellationToken)
    {
        var workflowDefinitionLabelCollection = _serviceProvider.GetService<IMongoCollection<WorkflowDefinitionLabel>>();
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