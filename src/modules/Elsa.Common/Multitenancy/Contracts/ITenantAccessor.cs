namespace Elsa.Common.Multitenancy;

public interface ITenantAccessor
{
    /// <summary>
    /// Get the current <see cref="Multitenancy.Tenant"/>.
    /// </summary>
    /// <returns>Current tenant or null.</returns>
    Tenant? Tenant { get; }

    IDisposable PushContext(Tenant? tenant);
}