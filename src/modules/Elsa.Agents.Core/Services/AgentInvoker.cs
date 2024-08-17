using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001

namespace Elsa.Agents;

public class AgentInvoker(KernelFactory kernelFactory, IKernelConfigProvider kernelConfigProvider)
{
    public async Task<InvokeAgentResult> InvokeAgentAsync(string agentName, IDictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        var kernelConfig = await kernelConfigProvider.GetKernelConfigAsync(cancellationToken);
        var kernel = kernelFactory.CreateKernel(kernelConfig, agentName);
        var agentConfig = kernelConfig.Agents[agentName];
        var executionSettings = agentConfig.ExecutionSettings;
        var promptExecutionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = executionSettings.Temperature,
            TopP = executionSettings.TopP,
            MaxTokens = executionSettings.MaxTokens,
            PresencePenalty = executionSettings.PresencePenalty,
            FrequencyPenalty = executionSettings.FrequencyPenalty,
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            ResponseFormat = executionSettings.ResponseFormat,
            ChatSystemPrompt = agentConfig.PromptTemplate,
        };
        
        var promptExecutionSettingsDictionary = new Dictionary<string, PromptExecutionSettings>
        {
            [PromptExecutionSettings.DefaultServiceId] = promptExecutionSettings,
        };
        
        var promptTemplateConfig = new PromptTemplateConfig
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
        
        var kernelFunction = kernel.CreateFunctionFromPrompt(promptTemplateConfig);
        var kernelArguments = new KernelArguments(input);
        var result = await kernelFunction.InvokeAsync(kernel, kernelArguments, cancellationToken: cancellationToken);
        return new(agentConfig, result);
    }
}