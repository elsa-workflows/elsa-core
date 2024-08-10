namespace Elsa.Agents.Options;

public class AgentsOptions
{
    public ICollection<ApiKeyConfig> ApiKeys { get; set; }
    public ICollection<ServiceProfileConfig> ServiceProfiles { get; set; }
    public ICollection<PluginConfig> Plugins { get; set; }
    public ICollection<SkillConfig> Skills { get; set; }
    public ICollection<AgentConfig> Agents { get; set; }
}