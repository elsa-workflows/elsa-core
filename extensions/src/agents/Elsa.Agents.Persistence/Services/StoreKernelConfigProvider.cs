using Elsa.Agents.Persistence.Contracts;

namespace Elsa.Agents.Persistence;

public class StoreKernelConfigProvider(IAgentStore agentStore) : IKernelConfigProvider
{
    public async Task<KernelConfig> GetKernelConfigAsync(CancellationToken cancellationToken = default)
    {
        var kernelConfig = new KernelConfig();
        var agents = await agentStore.ListAsync(cancellationToken);
        foreach (var agent in agents) kernelConfig.Agents[agent.Name] = agent.ToAgentConfig();
        return kernelConfig;
    }
}