using Elsa.Common.Entities;

namespace Elsa.Shells;

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