using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents.Core.ShellFeatures;

/// <summary>
/// Shell feature for agents core functionality.
/// </summary>
[ShellFeature(
    DisplayName = "Agents Core",
    Description = "Provides core agents functionality for AI-powered workflows")]
[UsedImplicitly]
public class AgentsCoreShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions<AgentsOptions>();
        services.AddScoped<IAgentInvoker, AgentInvoker>();
        services.AddScoped<IAgentFactory, AgentFactory>();
        services.AddScoped<ISkillDiscoverer, SkillDiscoverer>();
        services.AddScoped<KernelConfigProvider>();
    }
}

