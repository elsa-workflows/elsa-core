using Elsa.Api.Client.Resources.ExternalAuthentication.IdentityLinks.Models;
using Elsa.Api.Client.Resources.ExternalAuthentication.IdentityLinks.Requests;
using Elsa.Api.Client.Resources.ExternalAuthentication.IdentityLinks.Responses;
using Refit;

namespace Elsa.Api.Client.Resources.ExternalAuthentication.IdentityLinks.Contracts;

/// <summary>
/// Client for administrator-managed external identity links.
/// </summary>
public interface IExternalIdentityLinksApi
{
    [Get("/external-authentication/identity-links")]
    Task<ListExternalIdentityLinksResponse> ListAsync([Query] ListExternalIdentityLinksRequest request, CancellationToken cancellationToken = default);

    [Get("/external-authentication/user-options")]
    Task<FindIdentityLinkUsersResponse> FindUsersAsync([Query] FindIdentityLinkUsersRequest request, CancellationToken cancellationToken = default);

    [Post("/external-authentication/identity-links")]
    Task<ExternalIdentityLink> PrelinkAsync([Body] PrelinkExternalIdentityRequest request, CancellationToken cancellationToken = default);

    [Delete("/external-authentication/identity-links/{linkId}")]
    Task UnlinkAsync(string linkId, CancellationToken cancellationToken = default);
}
