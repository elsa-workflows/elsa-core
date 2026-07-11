using CShells.Lifecycle;
using Elsa.Common.Multitenancy;
using JetBrains.Annotations;

namespace Elsa.Common.ShellHandlers;

[UsedImplicitly]
public class ActivateShellTenants(ITenantService tenantService) : IShellInitializer, IDrainHandler
{
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return tenantService.ActivateTenantsAsync(cancellationToken);
    }

    public Task DrainAsync(IDrainExtensionHandle _, CancellationToken cancellationToken)
    {
        return tenantService.DeactivateTenantsAsync(cancellationToken);
    }
}
