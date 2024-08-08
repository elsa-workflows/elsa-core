using JetBrains.Annotations;

namespace Elsa.Agents;

[UsedImplicitly]
public class AgentConfig
{
    public string Name { get; set; } = default!;
    public ICollection<string> Models { get; set; } = new List<string>();
    public ICollection<string> Skills { get; set; } = new List<string>();
}