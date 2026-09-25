using Elsa.Common.Multitenancy;
using Elsa.Features.Services;
using Elsa.Features.Abstractions;
using Elsa.Connections.Contracts;
using Elsa.Connections.Services;
using Elsa.Features.Attributes;
using Elsa.Secrets.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Elsa.Connections.Features;

[DependsOn(typeof(SecretsFeature))]
public sealed class ConnectionsFeature(IModule module) : FeatureBase(module)
{
    public override void Apply()
    {
        // Lifecycle services always establish an explicit tenant context. Keep a default accessor for
        // non-multitenant hosts; TryAdd preserves the accessor supplied by MultitenancyFeature when enabled.
        Services.TryAddSingleton<ITenantAccessor, DefaultTenantAccessor>();
        Services.TryAddSingleton(TimeProvider.System);
        Services.TryAddScoped<IConnectionUseAuthorizer, DenyAllConnectionUseAuthorizer>();
        Services.AddOptions<ConnectionInspectionOptions>();
        // Persistence is optional. An inspector without a lifecycle store stays fail-closed.
        Services.TryAddScoped<IConnectionMetadataInspector>(sp => new DefaultConnectionMetadataInspector(
            sp.GetService<IConnectionLifecycleStore>(),
            sp.GetRequiredService<IConnectionUseAuthorizer>(),
            sp.GetRequiredService<ITenantAccessor>(),
            sp.GetRequiredService<IOptions<ConnectionInspectionOptions>>()));
        Services.TryAddScoped<DefaultConnectionLifecycleService>();
        Services.TryAddScoped<IConnectionLifecycleService>(sp => sp.GetRequiredService<DefaultConnectionLifecycleService>());
        Services.TryAddScoped<IStaticApiKeyLifecycleService>(sp => sp.GetRequiredService<DefaultConnectionLifecycleService>());
        Services.TryAddScoped<IConnectionBackgroundUseService>(sp => sp.GetRequiredService<DefaultConnectionLifecycleService>());
        Services.TryAddScoped<IConnectionLifecycleRecoveryService>(sp => sp.GetRequiredService<DefaultConnectionLifecycleService>());
    }
}
