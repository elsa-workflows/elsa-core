namespace Elsa.Common.Multitenancy;

public class TenantEventsManager(IEnumerable<ITenantActivatedEvent> tenantActivatedEvents, IEnumerable<ITenantDeactivatedEvent> tenantDeactivatedEvents)
{
    public async Task TenantActivatedAsync(TenantActivatedEventArgs args)
    {
        foreach (var tenantActivatedEvent in tenantActivatedEvents) await tenantActivatedEvent.TenantActivatedAsync(args);
    }

    public async Task TenantDeactivatedAsync(TenantDeactivatedEventArgs args)
    {
        foreach (var tenantDeactivatedEvent in tenantDeactivatedEvents) await tenantDeactivatedEvent.TenantDeactivatedAsync(args);
    }
}