using Elsa.Agents.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Agents;

public class ConfigurationKernelConfigProvider(IOptions<AgentsOptions> options) : IKernelConfigProvider
{
    public Task<KernelConfig> GetKernelConfigAsync(CancellationToken cancellationToken = default)
    {
        var kernelConfig = new KernelConfig();
        foreach (var apiKey in options.Value.ApiKeys) kernelConfig.ApiKeys[apiKey.Name] = apiKey;
        foreach (var serviceProfile in options.Value.Services) kernelConfig.Services[serviceProfile.Name] = serviceProfile;
        foreach (var agent in options.Value.Agents) kernelConfig.Agents[agent.Name] = agent;
        return Task.FromResult(kernelConfig);
    }
}