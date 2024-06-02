using Elsa.Common.Entities;
using Elsa.Tenants.Models;

namespace Elsa.Tenants;

/// <summary>
/// Represents a factory for creating Shell objects.
/// </summary>
public interface IShellFactory
{
    /// <summary>
    /// Creates a new Shell object for the specified tenant.
    /// </summary>
    /// <returns>A newly created Shell object.</returns>
    Shell CreateShell(Tenant tenant);
}