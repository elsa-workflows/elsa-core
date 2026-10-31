using Elsa.Studio.Security.Models;

namespace Elsa.Studio.Security.Contracts;

/// <summary>
/// Resolves the remote feature and effective permissions required by role administration.
/// </summary>
public interface IRoleAdministrationAccessService
{
    Task<RoleAdministrationAccess> GetAsync(CancellationToken cancellationToken = default);
    void Invalidate();
}
