using Elsa.Studio.Security.Models;

namespace Elsa.Studio.Security.Contracts;

/// <summary>
/// Resolves the remote feature and effective permissions required by user administration.
/// </summary>
public interface IUserAdministrationAccessService
{
    Task<UserAdministrationAccess> GetAsync(CancellationToken cancellationToken = default);
    void Invalidate();
}
