using Microsoft.Extensions.Hosting;

namespace Elsa.SemanticKernel.HostedServices;

public class ConfigureAgentManager(KernelConfig kernelConfig, AgentManager agentManager, KernelFactory kernelFactory, SkillExecutor skillExecutor) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var agentConfig in kernelConfig.Agents.Values)
        {
            var kernel = kernelFactory.CreateKernel(agentConfig);
            var agent = new Agent(agentConfig, kernel, skillExecutor);
            agentManager.AddAgent(agent);
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}