using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Agents;

/// <summary>
/// Provides kernel configuration from configuration.
/// </summary>
[UsedImplicitly]
public class KernelConfigProvider(IOptions<AgentsOptions> options) : IKernelConfigProvider
{
    public Task<KernelConfig> GetKernelConfigAsync(CancellationToken cancellationToken = default)
    {
        var kernelConfig = new KernelConfig();

        if (options.Value.Agents != null!)
        {
            foreach (var agent in options.Value.Agents)
                kernelConfig.Agents[agent.Name] = agent;
        }

        return Task.FromResult(kernelConfig);
    }
}