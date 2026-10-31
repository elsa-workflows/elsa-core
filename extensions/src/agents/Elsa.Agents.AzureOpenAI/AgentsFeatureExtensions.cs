using Microsoft.SemanticKernel;

namespace Elsa.Agents.AzureOpenAI;

public static class AgentsFeatureExtensions
{
    public static AgentsFeature AddAzureOpenAIChatCompletion(this AgentsFeature feature,
        string deploymentName,
        string endpoint,
        string apiKey,
        string? serviceId = null,
        string? modelId = null,
        string? apiVersion = null,
        HttpClient? httpClient = null,
        string name = "Azure OpenAI Chat Completion")
    {
        return feature.AddServiceDescriptor(new()
        {
            Name = name,
            ConfigureKernel = kernel => kernel.Services.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey, serviceId, modelId, apiVersion, httpClient)
        });
    }
}