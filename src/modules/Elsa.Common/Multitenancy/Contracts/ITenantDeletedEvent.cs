namespace Elsa.Common.Multitenancy;

public interface ITenantDeletedEvent
{
    Task TenantDeletedAsync(TenantDeletedEventArgs args);
}