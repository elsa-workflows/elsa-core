using Elsa.Api.Client.Resources.ExternalAuthentication.IdentityLinks.Models;

namespace Elsa.Api.Client.Resources.ExternalAuthentication.IdentityLinks.Responses;

/// <summary>
/// A cursor-paged external identity link response.
/// </summary>
public record ListExternalIdentityLinksResponse(IReadOnlyCollection<ExternalIdentityLink> Items, string? NextCursor);
