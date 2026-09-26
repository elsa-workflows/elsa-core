using Elsa.Studio.ExternalAuthentication.Models;
using Refit;

namespace Elsa.Studio.ExternalAuthentication.Client;

/// <summary>
/// Narrow role lookup used only to populate unlinked-identity policy selectors.
/// </summary>
public interface IIdentityRolesApi
{
    [Get("/identity/roles")]
    Task<IdentityRoleOptionsResponse> ListAsync(CancellationToken cancellationToken = default);
}
