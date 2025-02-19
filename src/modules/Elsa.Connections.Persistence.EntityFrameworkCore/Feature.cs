using Elsa.Connections.Persistence.Entities;
using Elsa.Connections.Persistence.Features;
using Elsa.EntityFrameworkCore;
using Elsa.EntityFrameworkCore.EntityHandlers;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Persistence.EntityFrameworkCore;

/// <summary>
/// Configures the default workflow runtime to use EF Core persistence providers.
/// </summary>
[DependsOn(typeof(ConnectionPersistenceFeature))]
public class EFCoreConnectionPersistenceFeature(IModule module) : PersistenceFeatureBase<EFCoreConnectionPersistenceFeature, ConnectionDbContext>(module)
{
    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<ConnectionPersistenceFeature>(feature =>
        {
            feature
                .UseConnectionStore(sp=> sp.GetRequiredService<EFCoreConnectionStore>())
            ;
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        AddEntityStore<ConnectionDefinition, EFCoreConnectionStore>();
    }
}