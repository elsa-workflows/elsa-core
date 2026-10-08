namespace Elsa.Common.Multitenancy;

public interface ITenantDeactivatedEvent
{
    Task TenantDeactivatedAsync(TenantDeactivatedEventArgs args);
}