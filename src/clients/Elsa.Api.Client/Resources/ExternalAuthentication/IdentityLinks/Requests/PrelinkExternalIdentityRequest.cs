namespace Elsa.Api.Client.Resources.ExternalAuthentication.IdentityLinks.Requests;

/// <summary>
/// Links an external identity tuple to an existing tenant user.
/// The subject is accepted only for this request and is never returned by the API.
/// </summary>
public record PrelinkExternalIdentityRequest(string UserId, string ConnectionId, string Issuer, string Subject);
