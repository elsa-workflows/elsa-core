using Elsa.Studio.Security.Models;
using Refit;

namespace Elsa.Studio.Security.Client;

/// <summary>Internal endpoint for the current caller's effective Identity grants.</summary>
public interface IMePermissionsApi
{
    [Get("/identity/me/permissions")]
    Task<CurrentCallerPermissionsResponse> GetAsync(CancellationToken cancellationToken = default);
}
