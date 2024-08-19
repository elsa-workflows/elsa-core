using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010

namespace Elsa.Agents;

public static class AgentConfigExtensions
{
    public static OpenAIPromptExecutionSettings ToOpenAIPromptExecutionSettings(this AgentConfig agentConfig)
    {
        return new OpenAIPromptExecutionSettings
        {
            Temperature = agentConfig.ExecutionSettings.Temperature,
            TopP = agentConfig.ExecutionSettings.TopP,
            MaxTokens = agentConfig.ExecutionSettings.MaxTokens,
            PresencePenalty = agentConfig.ExecutionSettings.PresencePenalty,
            FrequencyPenalty = agentConfig.ExecutionSettings.FrequencyPenalty,
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            ResponseFormat = agentConfig.ExecutionSettings.ResponseFormat,
            ChatSystemPrompt = agentConfig.PromptTemplate,
        };
    }

    public static PromptTemplateConfig ToPromptTemplateConfig(this AgentConfig agentConfig)
    {
        var promptExecutionSettingsDictionary = new Dictionary<string, PromptExecutionSettings>
        {
            [PromptExecutionSettings.DefaultServiceId] = agentConfig.ToOpenAIPromptExecutionSettings(),
        };

        return new PromptTemplateConfig
        {
            Name = agentConfig.FunctionName,
            Description = agentConfig.Description,
            Template = agentConfig.PromptTemplate,
            ExecutionSettings = promptExecutionSettingsDictionary,
            AllowDangerouslySetContent = true,
            InputVariables = agentConfig.InputVariables.Select(x => new InputVariable
            {
                Name = x.Name,
                Description = x.Description,
                IsRequired = true,
                AllowDangerouslySetContent = true
            }).ToList()
        };
    }
}