namespace Elsa.Api.Client.Resources.ExternalAuthentication.IdentityLinks.Models;

/// <summary>
/// Safe metadata for an external identity linked to an Elsa user.
/// </summary>
public record ExternalIdentityLink(
    string Id,
    string UserId,
    string ConnectionId,
    string Issuer,
    string? SubjectHint,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSignedInAt);
