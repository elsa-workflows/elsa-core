using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Agents;

[UsedImplicitly]
public class ConfigurationKernelConfigProvider(IOptions<AgentsOptions> options) : IKernelConfigProvider
{
    public Task<KernelConfig> GetKernelConfigAsync(CancellationToken cancellationToken = default)
    {
        var kernelConfig = new KernelConfig();
        foreach (var apiKey in options.Value.ApiKeys) kernelConfig.ApiKeys[apiKey.Name] = apiKey;
        foreach (var service in options.Value.Services) kernelConfig.Services[service.Name] = service;
        foreach (var agent in options.Value.Agents) kernelConfig.Agents[agent.Name] = agent;
        return Task.FromResult(kernelConfig);
    }
}