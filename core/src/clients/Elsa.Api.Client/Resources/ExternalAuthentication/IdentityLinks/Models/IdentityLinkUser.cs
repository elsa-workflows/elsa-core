namespace Elsa.Api.Client.Resources.ExternalAuthentication.IdentityLinks.Models;

/// <summary>
/// Minimal tenant-scoped user data for selecting a prelink target.
/// </summary>
public record IdentityLinkUser(string Id, string DisplayName);
