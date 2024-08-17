using Microsoft.Extensions.Hosting;

namespace Elsa.Agents.HostedServices;

public class ConfigureAgentManager(KernelConfig kernelConfig, AgentManager agentManager, KernelFactory kernelFactory, AgentInvoker agentInvoker) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var personaConfig in kernelConfig.Agents.Values)
        {
            var kernel = kernelFactory.CreateKernel(personaConfig);
            var agent = new Agent(personaConfig, kernel, agentInvoker);
            agentManager.AddAgent(agent);
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}