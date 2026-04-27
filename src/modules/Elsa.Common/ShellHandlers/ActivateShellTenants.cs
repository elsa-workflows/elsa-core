using CShells.Lifecycle;
using Elsa.Common.Multitenancy;
using JetBrains.Annotations;

namespace Elsa.Common.ShellHandlers;

/// <summary>
/// Activates the shell's tenants when the shell becomes <see cref="ShellLifecycleState.Active"/> and deactivates
/// them when the shell enters <see cref="ShellLifecycleState.Draining"/>.
/// </summary>
/// <remarks>
/// Migrated from the pre-0.0.15 CShells lifecycle API
/// (<c>IShellActivatedHandler</c>/<c>IShellDeactivatingHandler</c>) to the current
/// <see cref="IShellInitializer"/>/<see cref="IDrainHandler"/> primitives. Both register as transient via
/// <c>MultitenancyFeature</c>.
/// </remarks>
[UsedImplicitly]
public sealed class ActivateShellTenants(ITenantService tenantService) : IShellInitializer, IDrainHandler
{
    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return tenantService.ActivateTenantsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task DrainAsync(IDrainExtensionHandle extensionHandle, CancellationToken cancellationToken)
    {
        return tenantService.DeactivateTenantsAsync(cancellationToken);
    }
}
