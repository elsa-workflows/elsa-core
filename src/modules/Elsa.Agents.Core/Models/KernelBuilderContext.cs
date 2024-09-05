using JetBrains.Annotations;
using Microsoft.SemanticKernel;

namespace Elsa.Agents;

[UsedImplicitly]
public record KernelBuilderContext(IKernelBuilder KernelBuilder, KernelConfig KernelConfig, ServiceConfig ServiceConfig)
{
    public string GetApiKey()
    {
        var settings = ServiceConfig.Settings;
        if (settings.TryGetValue("ApiKey", out var apiKey))
            return (string)apiKey;

        if (settings.TryGetValue("ApiKeyRef", out var apiKeyRef))
            return KernelConfig.ApiKeys[(string)apiKeyRef].Value;

        throw new KeyNotFoundException($"No api key found for service {ServiceConfig.Type}");
    }
}