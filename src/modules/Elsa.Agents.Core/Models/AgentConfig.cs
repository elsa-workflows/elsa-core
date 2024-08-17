using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010

namespace Elsa.Agents;

public class AgentConfig
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ICollection<string> Services { get; set; }
    public string FunctionName { get; set; } = default!;
    public string PromptTemplate { get; set; } = default!;
    public List<InputVariableConfig> InputVariables { get; set; } = new ();
    public OutputVariableConfig OutputVariable { get; set; } = default!;
    public ExecutionSettingsConfig ExecutionSettings { get; set; } = default!;
    public ICollection<string> Plugins { get; set; } = [];
    public ICollection<string> Agents { get; set; } = [];
    
    public OpenAIPromptExecutionSettings ToOpenAIPromptExecutionSettings()
    {
        return new OpenAIPromptExecutionSettings
        {
            Temperature = ExecutionSettings.Temperature,
            TopP = ExecutionSettings.TopP,
            MaxTokens = ExecutionSettings.MaxTokens,
            PresencePenalty = ExecutionSettings.PresencePenalty,
            FrequencyPenalty = ExecutionSettings.FrequencyPenalty,
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            ResponseFormat = ExecutionSettings.ResponseFormat,
            ChatSystemPrompt = PromptTemplate,
        };
    }

    public PromptTemplateConfig ToPromptTemplateConfig()
    {
        var promptExecutionSettingsDictionary = new Dictionary<string, PromptExecutionSettings>
        {
            [PromptExecutionSettings.DefaultServiceId] = ToOpenAIPromptExecutionSettings(),
        };
        
        return new PromptTemplateConfig
        {
            Name = FunctionName,
            Description = Description,
            Template = PromptTemplate,
            ExecutionSettings = promptExecutionSettingsDictionary,
            AllowDangerouslySetContent = true,
            InputVariables = InputVariables.Select(x => new InputVariable
            {
                Name = x.Name,
                Description = x.Description,
                IsRequired = true,
                AllowDangerouslySetContent = true
            }).ToList()
        };
    }
}