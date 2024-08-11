namespace Elsa.Agents;

public class KernelConfig
{
    public IDictionary<string, ApiKeyConfig> ApiKeys { get; set; } = new Dictionary<string, ApiKeyConfig>();
    public IDictionary<string, ServiceProfileConfig> ServiceProfiles { get; } = new Dictionary<string, ServiceProfileConfig>();
    public IDictionary<string, PluginConfig> Plugins { get; } = new Dictionary<string, PluginConfig>();
    public IDictionary<string, SkillConfig> Skills { get; } = new Dictionary<string, SkillConfig>();
    public IDictionary<string, AgentConfig> Agents { get; } = new Dictionary<string, AgentConfig>();
}