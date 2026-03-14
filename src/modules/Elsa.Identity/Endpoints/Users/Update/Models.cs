namespace Elsa.Identity.Endpoints.Users.Update;

internal class Request
{
    public string? Password { get; set; }
    public ICollection<string>? Roles { get; set; }
}

internal record Response(
    string Id,
    string Name,
    ICollection<string> Roles,
    string? TenantId
);

