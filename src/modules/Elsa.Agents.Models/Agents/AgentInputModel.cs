using System.ComponentModel.DataAnnotations;

namespace Elsa.Agents;

public class AgentInputModel
{
    [Required] public string Name { get; set; } = "";
    [Required] public string Description { get; set; } = "";
    public ICollection<string> Services { get; set; } = [];
    [Required] public string FunctionName { get; set; } = "";
    [Required] public string PromptTemplate { get; set; } = "";
    public ICollection<InputVariableConfig> InputVariables { get; set; } = [];
    [Required] public OutputVariableConfig OutputVariable { get; set; } = new();
    public ExecutionSettingsConfig ExecutionSettings { get; set; } = new();
    public ICollection<string> Plugins { get; set; } = [];
    public ICollection<string> Agents { get; set; } = [];
}