using Elsa.Common.Entities;

namespace Elsa.Agents.Persistence.Entities;

public class AgentDefinition : Entity
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public AgentConfig AgentConfig { get; set; } = default!;
    public AgentConfig ToAgentConfig() => AgentConfig;
}