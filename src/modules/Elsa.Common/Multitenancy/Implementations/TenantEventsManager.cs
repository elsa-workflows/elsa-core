using Microsoft.Extensions.Logging;

namespace Elsa.Common.Multitenancy;

public class TenantEventsManager(IEnumerable<ITenantActivatedEvent> tenantActivatedEvents, IEnumerable<ITenantDeactivatedEvent> tenantDeactivatedEvents, ILogger<TenantEventsManager> logger)
{
    public async Task TenantActivatedAsync(TenantActivatedEventArgs args)
    {
        foreach (var tenantActivatedEvent in tenantActivatedEvents)
        {
            try
            {
                await tenantActivatedEvent.TenantActivatedAsync(args);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error occurred while processing tenant activated event.");
            }
        }
    }

    public async Task TenantDeactivatedAsync(TenantDeactivatedEventArgs args)
    {
        foreach (var tenantDeactivatedEvent in tenantDeactivatedEvents)
        {
            try
            {
                await tenantDeactivatedEvent.TenantDeactivatedAsync(args);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error occurred while processing tenant deactivated event.");
            }
        }
    }
}