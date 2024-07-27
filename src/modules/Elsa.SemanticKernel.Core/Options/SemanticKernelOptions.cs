namespace Elsa.SemanticKernel.Options;

public class SemanticKernelOptions
{
    public ICollection<ModelConfig> Models { get; set; }
    public ICollection<PluginConfig> Plugins { get; set; }
    public ICollection<SkillConfig> Skills { get; set; }
    public ICollection<AgentConfig> Agents { get; set; }
}