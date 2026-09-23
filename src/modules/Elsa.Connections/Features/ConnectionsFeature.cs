using Elsa.Features.Services;
using Elsa.Features.Abstractions;
using Elsa.Connections.Contracts;
using Elsa.Connections.Services;
using Elsa.Features.Attributes;
using Elsa.Secrets.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Connections.Features;

[DependsOn(typeof(SecretsFeature))]
public sealed class ConnectionsFeature(IModule module) : FeatureBase(module)
{
    public override void Apply()
    {
        Services.TryAddSingleton(TimeProvider.System);
        Services.TryAddScoped<IConnectionUseAuthorizer, DenyAllConnectionUseAuthorizer>();
        Services.TryAddScoped<DefaultConnectionLifecycleService>();
        Services.TryAddScoped<IConnectionLifecycleService>(sp => sp.GetRequiredService<DefaultConnectionLifecycleService>());
        Services.TryAddScoped<IConnectionBackgroundUseService>(sp => sp.GetRequiredService<DefaultConnectionLifecycleService>());
        Services.TryAddScoped<IConnectionLifecycleRecoveryService>(sp => sp.GetRequiredService<DefaultConnectionLifecycleService>());
    }
}
