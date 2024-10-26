using Elsa.Alterations.Core.Entities;
using Elsa.MongoDb.Helpers;
using Elsa.MongoDb.Modules.Alterations.Documents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace Elsa.MongoDb.Modules.Alterations;

internal class CreateIndices(IServiceProvider serviceProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        return Task.WhenAll(
            CreateAlterationPlanIndices(scope, cancellationToken),
            CreateAlterationJobIndices(scope, cancellationToken));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static Task CreateAlterationPlanIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var alterationPlanCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<AlterationPlanDocument>>();
        if (alterationPlanCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            alterationPlanCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    [
                        new(indexBuilder.Ascending(x => new
                        {
                            x.Id,
                            x.TenantId
                        }), new CreateIndexOptions
                        {
                            Unique = true
                        }),
                        new(indexBuilder.Ascending(x => x.Status)),
                        new(indexBuilder.Ascending(x => x.CreatedAt)),
                        new(indexBuilder.Ascending(x => x.StartedAt)),
                        new(indexBuilder.Ascending(x => x.CompletedAt))
                    ],
                    cancellationToken));
    }

    private static Task CreateAlterationJobIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var alterationJobCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<AlterationJob>>();
        if (alterationJobCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            alterationJobCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    [
                        new(indexBuilder.Ascending(x => new
                        {
                            x.Id,
                            x.TenantId
                        }), new CreateIndexOptions
                        {
                            Unique = true
                        }),
                        new(indexBuilder.Ascending(x => x.PlanId)),
                        new(indexBuilder.Ascending(x => x.WorkflowInstanceId)),
                        new(indexBuilder.Ascending(x => x.Status)),
                        new(indexBuilder.Ascending(x => x.CreatedAt)),
                        new(indexBuilder.Ascending(x => x.StartedAt)),
                        new(indexBuilder.Ascending(x => x.CompletedAt))
                    ],
                    cancellationToken));
    }
}