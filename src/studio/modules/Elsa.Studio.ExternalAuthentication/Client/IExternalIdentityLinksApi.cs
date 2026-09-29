using Elsa.Studio.ExternalAuthentication.Models;
using Refit;

namespace Elsa.Studio.ExternalAuthentication.Client;

public interface IExternalIdentityLinksApi
{
    [Get("/external-authentication/identity-links")]
    Task<ListExternalIdentityLinksResponse> ListAsync(
        string? userId = null,
        string? connectionKey = null,
        string? cursor = null,
        int pageSize = 25,
        CancellationToken cancellationToken = default);

    [Get("/external-authentication/user-options")]
    Task<FindIdentityLinkUsersResponse> FindUsersAsync(
        string? search = null,
        string? cursor = null,
        int pageSize = 25,
        CancellationToken cancellationToken = default);

    [Post("/external-authentication/identity-links")]
    Task<ExternalIdentityLink> PrelinkAsync([Body] PrelinkExternalIdentityRequest request, CancellationToken cancellationToken = default);

    [Post("/external-authentication/identity-links/{linkId}/replace")]
    Task<ExternalIdentityLink> ReplaceAsync(string linkId, [Body] ReplaceExternalIdentityLinkRequest request, CancellationToken cancellationToken = default);

    [Delete("/external-authentication/identity-links/{linkId}")]
    Task UnlinkAsync(string linkId, CancellationToken cancellationToken = default);
}
