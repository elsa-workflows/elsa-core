using Elsa.Common.Multitenancy;

namespace Elsa.Common.Multitenancy.EventHandlers;

/// <summary>
/// Adapts tenant lifecycle events to the tenant task lifecycle coordinator.
/// </summary>
public class TenantTaskLifecycleEventHandler(TenantTaskLifecycleCoordinator coordinator) : ITenantActivatedEvent, ITenantDeactivatedEvent
{
    public Task TenantActivatedAsync(TenantActivatedEventArgs args) => coordinator.ActivateTenantAsync(args);
    public Task TenantDeactivatedAsync(TenantDeactivatedEventArgs args) => coordinator.DeactivateTenantAsync(args);
}
