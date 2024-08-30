using System.ComponentModel.DataAnnotations;

namespace Elsa.Agents;

public class AgentInputModel
{
    [Required] public string Name { get; set; } = default!;
    [Required] public string Description { get; set; } = "";
    public ICollection<string> Services { get; set; } = [];
    [Required] public string FunctionName { get; set; } = default!;
    [Required] public string PromptTemplate { get; set; } = default!;
    public ICollection<InputVariableConfig> InputVariables { get; set; } = [];
    [Required] public OutputVariableConfig OutputVariable { get; set; } = default!;
    public ExecutionSettingsConfig ExecutionSettings { get; set; } = new();
    public ICollection<string> Plugins { get; set; } = [];
    public ICollection<string> Agents { get; set; } = [];
}