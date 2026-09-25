using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0110

namespace Elsa.Agents;

public class AgentInvoker(IKernelConfigProvider kernelConfigProvider, IAgentFactory agentFactory) : IAgentInvoker
{
    /// <summary>
    /// Invokes an agent using the Microsoft Agent Framework (new approach).
    /// </summary>
    public async Task<InvokeAgentResult> InvokeAsync(InvokeAgentRequest request)
    {
        var kernelConfig = await kernelConfigProvider.GetKernelConfigAsync(request.CancellationToken);
        var agentConfig = kernelConfig.Agents[request.AgentName];

        // Create agent using Agent Framework
        var agent = agentFactory.CreateAgent(agentConfig);

        // Use provided chat history or create new one
        ChatHistory chatHistory = request.ChatHistory ?? [];

        // Format and add user input
        var promptTemplateConfig = new PromptTemplateConfig
        {
            Template = agentConfig.PromptTemplate,
            TemplateFormat = "handlebars",
            Name = "Run",
            AllowDangerouslySetContent = true,
        };

        var templateFactory = new HandlebarsPromptTemplateFactory();
        var promptTemplate = templateFactory.Create(promptTemplateConfig);
        var executionSettings = agentConfig.ExecutionSettings;
        var promptExecutionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = executionSettings.Temperature,
            TopP = executionSettings.TopP,
            MaxTokens = executionSettings.MaxTokens,
            PresencePenalty = executionSettings.PresencePenalty,
            FrequencyPenalty = executionSettings.FrequencyPenalty,
            ResponseFormat = executionSettings.ResponseFormat,
            ChatSystemPrompt = agentConfig.PromptTemplate,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };
        
        var promptExecutionSettingsDictionary = new Dictionary<string, PromptExecutionSettings>
        {
            [PromptExecutionSettings.DefaultServiceId] = promptExecutionSettings,
        };
        
        var kernelArguments = new KernelArguments(request.Input, promptExecutionSettingsDictionary);
        var renderedPrompt = await promptTemplate.RenderAsync(agent.Kernel, kernelArguments, request.CancellationToken);

        chatHistory.AddUserMessage(renderedPrompt);

        if (executionSettings.ResponseFormat == "json_object")
        {
            chatHistory.AddSystemMessage(
                """"
                  You are a function that returns *only* JSON.

                  Rules:
                  - Return a single valid JSON object.
                  - Do not add explanations.
                  - Do not add code fences.
                  - Do not prefix with ```json or any other markers.
                  - Output must start with { and end with }.
                  
                  If there's a problem with the JSON input, include the exact JSON input in your response for troubleshooting.
                """");
        }

        // Get response from agent
        var response = await agent.InvokeAsync(chatHistory, cancellationToken: request.CancellationToken).LastOrDefaultAsync(request.CancellationToken);

        if (response == null)
            throw new InvalidOperationException("Agent did not produce a response");

        return new(agentConfig, response);
    }
}