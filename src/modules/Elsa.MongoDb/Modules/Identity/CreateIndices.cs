using Elsa.Identity.Entities;
using Elsa.MongoDb.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace Elsa.MongoDb.Modules.Identity;

internal class CreateIndices(IServiceProvider serviceProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        return Task.WhenAll(
            CreateApplicationIndices(scope, cancellationToken),
            CreateUserIndices(scope, cancellationToken),
            CreateRoleIndices(scope, cancellationToken));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static Task CreateApplicationIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var applicationCollection = serviceScope.ServiceProvider.GetService<MongoCollectionBase<Application>>();
        if (applicationCollection == null) return Task.CompletedTask;
        
        return IndexHelpers.CreateAsync(
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
    
    private static Task CreateUserIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var userCollection = serviceScope.ServiceProvider.GetService<MongoCollectionBase<User>>();
        if (userCollection == null) return Task.CompletedTask;
        
        return IndexHelpers.CreateAsync(
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

    private static Task CreateRoleIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var roleCollection = serviceScope.ServiceProvider.GetService<MongoCollectionBase<Role>>();
        if (roleCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
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
}