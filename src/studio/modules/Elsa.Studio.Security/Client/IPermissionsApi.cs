using Elsa.Studio.Security.Models;
using Refit;

namespace Elsa.Studio.Security.Client;

/// <summary>Internal Elsa Identity permission catalog and wildcard reach endpoints.</summary>
public interface IPermissionsApi
{
    [Get("/identity/permissions")]
    Task<PermissionCatalogResponse> ListAsync(CancellationToken cancellationToken = default);

    [Get("/identity/permissions/reach")]
    Task<PermissionReachResponse> GetReachAsync([Query] string resource, CancellationToken cancellationToken = default);
}
