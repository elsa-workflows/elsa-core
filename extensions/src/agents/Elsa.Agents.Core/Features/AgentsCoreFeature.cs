using Elsa.Agents.Skills;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents.Features;

/// <summary>
/// A feature that installs API endpoints to interact with skilled agents.
/// </summary>
[UsedImplicitly]
public class AgentsCoreFeature(IModule module) : FeatureBase(module)
{
    private Func<IServiceProvider, IKernelConfigProvider> _kernelConfigProviderFactory = sp => sp.GetRequiredService<KernelConfigProvider>();
    
    public AgentsCoreFeature UseKernelConfigProvider(Func<IServiceProvider, IKernelConfigProvider> factory)
    {
        _kernelConfigProviderFactory = factory;
        return this;
    }
    
    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddOptions<AgentsOptions>();

        Services
            .AddScoped<IAgentInvoker, AgentInvoker>()
            .AddScoped<IAgentFactory, AgentFactory>()
            .AddScoped<ISkillDiscoverer, SkillDiscoverer>()
            .AddScoped(_kernelConfigProviderFactory)
            .AddScoped<KernelConfigProvider>()
            .AddSkillsProvider<ImageGeneratorSkillsProvider>()
            .AddSkillsProvider<DocumentQuerySkillsProvider>()
            ;
    }
}