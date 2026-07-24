using Elsa.Api.Client.Resources.ExternalAuthentication.IdentityLinks.Models;

namespace Elsa.Api.Client.Resources.ExternalAuthentication.IdentityLinks.Responses;

/// <summary>
/// A cursor-paged minimal user picker response.
/// </summary>
public record FindIdentityLinkUsersResponse(IReadOnlyCollection<IdentityLinkUser> Items, string? NextCursor);
