namespace Elsa.Identity.Endpoints.Roles.Create;

internal class Request
{
    public string? Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public ICollection<string>? Permissions { get; set; }
}

internal record Response(
    string Id,
    string Name,
    ICollection<string> Permissions
);