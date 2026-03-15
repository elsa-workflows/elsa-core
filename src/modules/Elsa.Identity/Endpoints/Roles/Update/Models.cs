namespace Elsa.Identity.Endpoints.Roles.Update;

internal class Request
{
    public string? Name { get; set; }
    public ICollection<string>? Permissions { get; set; }
}

internal record Response(
    string Id,
    string Name,
    ICollection<string> Permissions,
    string? TenantId
);

