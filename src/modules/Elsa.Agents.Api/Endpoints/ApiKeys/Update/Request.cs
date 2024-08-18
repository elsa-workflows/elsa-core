namespace Elsa.Agents.Api.Endpoints.ApiKeys.Update;

public class Request
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Value { get; set; }= default!;
}