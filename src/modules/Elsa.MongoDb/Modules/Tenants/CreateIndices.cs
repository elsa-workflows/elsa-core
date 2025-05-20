using Elsa.Common.Multitenancy;
using Elsa.MongoDb.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace Elsa.MongoDb.Modules.Tenants;

internal class CreateIndices(IServiceProvider serviceProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        return CreateTenantIndices(scope, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static Task CreateTenantIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var mongoCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<Tenant>>();
        if (mongoCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            mongoCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<Tenant>>
                    {
                        new(indexBuilder.Ascending(x => x.Name)),
                        new(indexBuilder.Ascending(x => x.TenantId))
                    },
                    cancellationToken));
    }
}