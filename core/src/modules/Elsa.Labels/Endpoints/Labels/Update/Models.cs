namespace Elsa.Labels.Endpoints.Labels.Update;

public class Request
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Color { get; set; }
}

public class Response
{
    public string Id { get; set; } = default!;
    public string NormalizedName { get; set; } = default!;
    public string? Description { get; set; }
    public string? Color { get; set; }
}