using Elsa.Identity.Entities;
using Elsa.MongoDb.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace Elsa.MongoDb.Modules.Identity;

internal class CreateIndices : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public CreateIndices(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.WhenAll(
            CreateApplicationIndices(cancellationToken),
            CreateUserIndices(cancellationToken),
            CreateRoleIndices(cancellationToken));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private Task CreateApplicationIndices(CancellationToken cancellationToken)
    {
        var applicationCollection = _serviceProvider.GetService<IMongoCollection<Application>>();
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
    
    private Task CreateUserIndices(CancellationToken cancellationToken)
    {
        var userCollection = _serviceProvider.GetService<IMongoCollection<User>>();
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

    private Task CreateRoleIndices(CancellationToken cancellationToken)
    {
        var roleCollection = _serviceProvider.GetService<IMongoCollection<Role>>();
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