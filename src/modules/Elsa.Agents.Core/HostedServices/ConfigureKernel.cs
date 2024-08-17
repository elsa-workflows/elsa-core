using Elsa.Agents.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Agents.HostedServices;

public class ConfigureKernel(KernelConfig kernelConfig, IOptions<AgentsOptions> options) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var apiKey in options.Value.ApiKeys) kernelConfig.ApiKeys[apiKey.Name] = apiKey;
        foreach (var serviceProfile in options.Value.Services) kernelConfig.Services[serviceProfile.Name] = serviceProfile;
        foreach (var plugin in options.Value.Plugins) kernelConfig.Plugins[plugin.Name] = plugin;
        foreach (var agent in options.Value.Agents) kernelConfig.Agents[agent.Name] = agent;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}