namespace Elsa.Api.Client.Resources.ExternalAuthentication.IdentityLinks.Requests;

/// <summary>
/// Atomically replaces an external identity link with a newly created link.
/// The subject is accepted only for this request and is never returned by the API.
/// </summary>
public record ReplaceExternalIdentityLinkRequest(string UserId, string ConnectionKey, string Issuer, string Subject);
