namespace Elsa.Identity.Endpoints.Applications.Create;

internal class Request
{
    public string Name { get; set; } = default!;
    public ICollection<string>? Roles { get; set; }
}

internal record Response(string ApiKey);