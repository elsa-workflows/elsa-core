using Microsoft.Extensions.Logging;

namespace Elsa.Common.Multitenancy;

public class TenantEventsManager(
    IEnumerable<ITenantActivatedEvent> tenantActivatedEvents, 
    IEnumerable<ITenantDeactivatedEvent> tenantDeactivatedEvents,
    IEnumerable<ITenantDeletedEvent> tenantDeletedEvents,
    ILogger<TenantEventsManager> logger)
{
    public async Task TenantActivatedAsync(TenantActivatedEventArgs args)
    {
        await ExecuteEventHandlersAsync(
            tenantActivatedEvents,
            (handler, eventArgs) => handler.TenantActivatedAsync(eventArgs),
            args,
            "activated",
            rethrow: true);
    }

    public async Task TenantDeactivatedAsync(TenantDeactivatedEventArgs args)
    {
        await ExecuteEventHandlersAsync(
            tenantDeactivatedEvents,
            (handler, eventArgs) => handler.TenantDeactivatedAsync(eventArgs),
            args,
            "deactivated");
    }
    
    public async Task TenantDeletedAsync(TenantDeletedEventArgs args)
    {
        await ExecuteEventHandlersAsync(
            tenantDeletedEvents,
            (handler, eventArgs) => handler.TenantDeletedAsync(eventArgs),
            args,
            "deleted");
    }

    private async Task ExecuteEventHandlersAsync<THandler, TArgs>(
        IEnumerable<THandler> handlers,
        Func<THandler, TArgs, Task> handlerAction,
        TArgs args,
        string eventType,
        bool rethrow = false)
    {
        foreach (var handler in handlers)
        {
            try
            {
                await handlerAction(handler, args);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error occurred while processing tenant {EventType} event.", eventType);

                // A failed activation (e.g. a migration startup task) leaves the tenant half-initialized, so surface it instead of continuing.
                if (rethrow)
                    throw;
            }
        }
    }
}