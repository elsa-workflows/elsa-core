using Microsoft.Extensions.Logging;

namespace Elsa.Common.Multitenancy;

public class TenantEventsManager(
    IEnumerable<ITenantActivatedEvent> tenantActivatedEvents, 
    IEnumerable<ITenantDeactivatedEvent> tenantDeactivatedEvents,
    IEnumerable<ITenantDeletedEvent> tenantDeletedEvents,
    IEnumerable<IAllTenantsActivatedEvent> allTenantsActivatedEvents,
    ILogger<TenantEventsManager> logger)
{
    [Obsolete("Use the constructor that accepts IAllTenantsActivatedEvent handlers instead. Removed in the next major version.")]
    public TenantEventsManager(
        IEnumerable<ITenantActivatedEvent> tenantActivatedEvents,
        IEnumerable<ITenantDeactivatedEvent> tenantDeactivatedEvents,
        IEnumerable<ITenantDeletedEvent> tenantDeletedEvents,
        ILogger<TenantEventsManager> logger) : this(tenantActivatedEvents, tenantDeactivatedEvents, tenantDeletedEvents, [], logger)
    {
    }

    public async Task TenantActivatedAsync(TenantActivatedEventArgs args)
    {
        await ExecuteEventHandlersAsync(
            tenantActivatedEvents,
            (handler, eventArgs) => handler.TenantActivatedAsync(eventArgs),
            args,
            args.CancellationToken,
            "activated",
            rethrow: true);
    }

    /// <summary>Runs the <see cref="IAllTenantsActivatedEvent"/> handlers; failures are logged, cancellation propagates.</summary>
    public async Task AllTenantsActivatedAsync(CancellationToken cancellationToken)
    {
        foreach (var handler in allTenantsActivatedEvents)
        {
            try
            {
                await handler.AllTenantsActivatedAsync(cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                logger.LogError(e, "Error occurred while processing {EventType} event.", "all tenants activated");
            }
        }
    }

    public async Task TenantDeactivatedAsync(TenantDeactivatedEventArgs args)
    {
        await ExecuteEventHandlersAsync(
            tenantDeactivatedEvents,
            (handler, eventArgs) => handler.TenantDeactivatedAsync(eventArgs),
            args,
            args.CancellationToken,
            "deactivated");
    }
    
    public async Task TenantDeletedAsync(TenantDeletedEventArgs args)
    {
        await ExecuteEventHandlersAsync(
            tenantDeletedEvents,
            (handler, eventArgs) => handler.TenantDeletedAsync(eventArgs),
            args,
            args.CancellationToken,
            "deleted");
    }

    private async Task ExecuteEventHandlersAsync<THandler, TArgs>(
        IEnumerable<THandler> handlers,
        Func<THandler, TArgs, Task> handlerAction,
        TArgs args,
        CancellationToken cancellationToken,
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
                if (!(e is OperationCanceledException && cancellationToken.IsCancellationRequested))
                {
                    logger.LogError(e, "Error occurred while processing tenant {EventType} event.", eventType);
                }

                // A failed activation (e.g. a migration startup task) leaves the tenant half-initialized, so surface it instead of continuing.
                if (rethrow)
                {
                    throw;
                }
            }
        }
    }
}