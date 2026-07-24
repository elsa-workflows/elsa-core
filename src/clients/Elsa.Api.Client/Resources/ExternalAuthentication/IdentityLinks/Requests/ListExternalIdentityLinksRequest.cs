namespace Elsa.Api.Client.Resources.ExternalAuthentication.IdentityLinks.Requests;

/// <summary>
/// Filters a cursor-paged external identity link list.
/// </summary>
public class ListExternalIdentityLinksRequest
{
    public string? UserId { get; set; }
    public string? ConnectionId { get; set; }
    public string? Cursor { get; set; }
    public int? PageSize { get; set; }
}
