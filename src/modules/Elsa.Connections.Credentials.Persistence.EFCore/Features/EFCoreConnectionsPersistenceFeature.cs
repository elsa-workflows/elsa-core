using Elsa.Connections.Contracts;
using Elsa.Connections.Features;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Persistence.EFCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.EFCore.Features;

[DependsOn(typeof(ConnectionsFeature))]
public sealed class EFCoreConnectionsPersistenceFeature(IModule module)
    : PersistenceFeatureBase<EFCoreConnectionsPersistenceFeature, ConnectionsElsaDbContext>(module)
{
    public override void Apply()
    {
        base.Apply();
        Services.AddScoped<EFCoreConnectionLifecycleStore>();
        Services.AddScoped<IConnectionLifecycleStore>(sp => sp.GetRequiredService<EFCoreConnectionLifecycleStore>());
    }
}
