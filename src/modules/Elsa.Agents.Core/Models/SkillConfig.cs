namespace Elsa.Agents;

public class SkillConfig
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ICollection<FunctionConfig> Functions { get; set; } = [];
    public ICollection<string> Plugins { get; set; } = [];
}