namespace Elsa.Agents;

public class AgentConfig
{
    public string Name { get; set; } = "";
    public string? FunctionName { get; set; }
    public string Description { get; set; } = "";
    public string PromptTemplate { get; set; } = null!;
    public ICollection<InputVariableConfig> InputVariables { get; set; } = [];
    public OutputVariableConfig OutputVariable { get; set; } = new();
    public ExecutionSettingsConfig ExecutionSettings { get; set; } = new();
    public ICollection<string> Skills { get; set; } = [];
    public ICollection<string> Agents { get; set; } = [];


}