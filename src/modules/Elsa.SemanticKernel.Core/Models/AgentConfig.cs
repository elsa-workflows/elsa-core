namespace Elsa.SemanticKernel;

public class AgentConfig
{
    public string Name { get; set; }
    public ICollection<string> Models { get; set; }
    public ICollection<string> Skills { get; set; }
}