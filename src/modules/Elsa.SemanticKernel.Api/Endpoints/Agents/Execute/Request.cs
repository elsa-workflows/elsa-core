namespace Elsa.SemanticKernel.Api.Endpoints.Agents.Execute;

public class Request
{
    public string Agent { get; set; }
    public string Skill { get; set; }
    public string Function { get; set; }
    public IDictionary<string, object?> Inputs { get; set; }
}