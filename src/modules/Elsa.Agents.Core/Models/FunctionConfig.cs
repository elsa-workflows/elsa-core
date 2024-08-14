namespace Elsa.Agents;

public class FunctionConfig
{
    public string FunctionName { get; set; } = default!;
    public string PromptTemplate { get; set; } = default!;
    public string Description { get; set; } = default!;
    public List<InputVariableConfig> InputVariables { get; set; } = new ();
    public OutputVariableConfig OutputVariable { get; set; } = default!;
    public ExecutionSettingsConfig ExecutionSettings { get; set; } = default!;
    
}