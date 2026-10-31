namespace Elsa.Api.Client.Resources.ExternalAuthentication.IdentityLinks.Requests;

/// <summary>
/// Searches the minimal tenant-scoped user picker data used for external identity links.
/// </summary>
public class FindIdentityLinkUsersRequest
{
    public string? Search { get; set; }
    public string? Cursor { get; set; }
    public int? PageSize { get; set; }
}
