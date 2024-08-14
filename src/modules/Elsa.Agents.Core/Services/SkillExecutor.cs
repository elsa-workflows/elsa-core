using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001

namespace Elsa.Agents;

public class SkillExecutor(KernelConfig kernelConfig)
{
    public async Task<ExecuteFunctionResult> ExecuteFunctionAsync(Kernel kernel, string skillName, string functionName, IDictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var skill = kernelConfig.Skills[skillName];
        var function = skill.Functions.First(f => f.FunctionName == functionName)!;
        var executionSettings = function.ExecutionSettings;
        var promptExecutionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = executionSettings.Temperature,
            TopP = executionSettings.TopP,
            MaxTokens = executionSettings.MaxTokens,
            PresencePenalty = executionSettings.PresencePenalty,
            FrequencyPenalty = executionSettings.FrequencyPenalty,
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            ResponseFormat = executionSettings.ResponseFormat,
            ChatSystemPrompt = function.PromptTemplate,
        };
        
        var promptExecutionSettingsDictionary = new Dictionary<string, PromptExecutionSettings>
        {
            [PromptExecutionSettings.DefaultServiceId] = promptExecutionSettings,
        };
        
        var promptTemplateConfig = new PromptTemplateConfig
        {
            Name = functionName,
            Description = function.Description,
            Template = function.PromptTemplate,
            ExecutionSettings = promptExecutionSettingsDictionary,
            AllowDangerouslySetContent = true,
            InputVariables = function.InputVariables.Select(x => new InputVariable
            {
                Name = x.Name,
                Description = x.Description,
                IsRequired = true,
                AllowDangerouslySetContent = true
            }).ToList()
        };
        
        var kernelFunction = kernel.CreateFunctionFromPrompt(promptTemplateConfig);
        var kernelArguments = new KernelArguments(input);
        var result = await kernelFunction.InvokeAsync(kernel, kernelArguments, cancellationToken: cancellationToken);
        return new(function, result);
    }
}