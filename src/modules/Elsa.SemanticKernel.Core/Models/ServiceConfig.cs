namespace Elsa.SemanticKernel;

public class ServiceConfig
{
    public string Type { get; set; }
    public IDictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
}