namespace Elsa.Agents.Api.Endpoints.ApiKeys.Create;

public class Request
{
    public string Name { get; set; } = default!;
    public string Value { get; set; }= default!;
}