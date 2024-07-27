namespace Elsa.SemanticKernel;

public class KernelConfig
{
    public IDictionary<string, ModelConfig> Models { get; } = new Dictionary<string, ModelConfig>();
    public IDictionary<string, PluginConfig> Plugins { get; } = new Dictionary<string, PluginConfig>();
    public IDictionary<string, SkillConfig> Skills { get; } = new Dictionary<string, SkillConfig>();
    public IDictionary<string, AgentConfig> Agents { get; } = new Dictionary<string, AgentConfig>();
}