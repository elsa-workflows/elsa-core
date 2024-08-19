namespace Elsa.Agents;

public class AgentConfig
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ICollection<string> Services { get; set; } = [];
    public string FunctionName { get; set; } = default!;
    public string PromptTemplate { get; set; } = default!;
    public ICollection<InputVariableConfig> InputVariables { get; set; } = [];
    public OutputVariableConfig OutputVariable { get; set; } = default!;
    public ExecutionSettingsConfig ExecutionSettings { get; set; } = default!;
    public ICollection<string> Plugins { get; set; } = [];
    public ICollection<string> Agents { get; set; } = [];
    
    
}