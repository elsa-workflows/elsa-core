namespace Elsa.Agents.Api.Endpoints.Agents.Invoke;

public class Request
{
    public string Agent { get; set; }
    public IDictionary<string, object?> Inputs { get; set; }
}