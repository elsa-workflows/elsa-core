using CShells.Hosting;
using Elsa.Common.Multitenancy;
using JetBrains.Annotations;

namespace Elsa.Common.ShellHandlers;

[UsedImplicitly]
public class ActivateShellTenants(ITenantService tenantService) : IShellActivatedHandler, IShellDeactivatingHandler
{
    public Task OnActivatedAsync(CancellationToken cancellationToken = default)
    {
        return tenantService.ActivateTenantsAsync(cancellationToken);
    }

    public Task OnDeactivatingAsync(CancellationToken cancellationToken = default)
    {
        return tenantService.DeactivateTenantsAsync(cancellationToken);
    }
}