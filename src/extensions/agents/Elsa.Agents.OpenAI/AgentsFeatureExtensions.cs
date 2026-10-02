using Microsoft.SemanticKernel;

namespace Elsa.Agents.OpenAI;

public static class AgentsFeatureExtensions
{
    public static AgentsFeature AddOpenAIChatCompletion(this AgentsFeature feature,
        string modelId,
        string apiKey,
        string? orgId = null,
        string? serviceId = null,
        string name = "OpenAI Chat Completion")
    {
        return feature.AddServiceDescriptor(new()
        {
            Name = name,
            ConfigureKernel = kernel => kernel.Services.AddOpenAIChatCompletion(modelId, apiKey, orgId, serviceId)
        });
    }
}