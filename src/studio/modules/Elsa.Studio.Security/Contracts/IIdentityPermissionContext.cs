using Elsa.Studio.Security.Models;

namespace Elsa.Studio.Security.Contracts;

/// <summary>
/// Provides a fail-closed snapshot of the current caller's effective Identity permissions.
/// </summary>
public interface IIdentityPermissionContext
{
    Task<IdentityPermissionSnapshot> GetAsync(CancellationToken cancellationToken = default);
    void Invalidate();
}
