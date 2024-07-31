namespace Elsa.Agents;

public class AgentDefinition
{
    public string Name { get; set; } = default!;
    public ICollection<string> Models { get; set; } = [];
    public ICollection<string> Skills { get; set; } = [];
}