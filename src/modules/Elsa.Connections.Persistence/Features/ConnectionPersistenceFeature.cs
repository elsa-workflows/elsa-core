using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Elsa.Connections.Persistence.Contracts;
using Elsa.Connections.Persistence.Services;
using Elsa.Connections.Persistence.Entities;

namespace Elsa.Connections.Persistence.Features;

public class ConnectionPersistenceFeature(IModule module) : FeatureBase(module)
{
    private Func<IServiceProvider, IConnectionStore> _connectionStoreFactory = sp => sp.GetRequiredService<InMemoryConnectionStore>();

    public ConnectionPersistenceFeature UseConnectionStore(Func<IServiceProvider, IConnectionStore> factory)
    {
        _connectionStoreFactory = factory;
        return this;
    }

    public override void Apply()
    {
        Services.AddScoped(_connectionStoreFactory);

        Services
            .AddMemoryStore<ConnectionDefinition, InMemoryConnectionStore>();
    }
}