using Elsa.Framework.Tenants;

namespace Elsa.Framework.Shells;

/// <summary>
/// Represents a factory for creating Shell objects.
/// </summary>
public interface ITenantShellFactory
{
    /// <summary>
    /// Creates a new Shell object for the specified tenant.
    /// </summary>
    /// <returns>A newly created <see cref="Shell"/> object.</returns>
    Shell CreateShell(Tenant tenant);
}