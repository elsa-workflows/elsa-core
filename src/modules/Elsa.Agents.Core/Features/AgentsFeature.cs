using Elsa.Agents.Options;
using Elsa.Agents.Plugins;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents.Features;

/// A feature that installs API endpoints to interact with skilled agents.
[UsedImplicitly]
public class AgentsFeature(IModule module) : FeatureBase(module)
{
    private Func<IServiceProvider, IKernelConfigProvider> _kernelConfigProviderFactory = sp => sp.GetRequiredService<ConfigurationKernelConfigProvider>();
    
    public AgentsFeature UseKernelConfigProvider(Func<IServiceProvider, IKernelConfigProvider> factory)
    {
        _kernelConfigProviderFactory = factory;
        return this;
    }
    
    /// <inheritdoc />
    public override void Configure()
    {
        Services.AddOptions<AgentsOptions>();

        Services
            .AddScoped<KernelFactory>()
            .AddScoped<AgentInvoker>()
            .AddScoped<IPluginDiscoverer, PluginDiscoverer>()
            .AddScoped<IServiceDiscoverer, ServiceDiscoverer>()
            .AddScoped(_kernelConfigProviderFactory)
            .AddScoped<ConfigurationKernelConfigProvider>()
            .AddPluginProvider<ImageGeneratorPluginProvider>()
            .AddAgentServiceProvider<OpenAIChatCompletionProvider>()
            .AddAgentServiceProvider<OpenAITextToImageProvider>()
            ;
    }
}