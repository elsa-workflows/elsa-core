namespace Elsa.Common.Multitenancy;

public interface ITenantActivatedEvent
{
    Task TenantActivatedAsync(TenantActivatedEventArgs args);
}