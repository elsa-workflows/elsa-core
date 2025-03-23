using Elsa.Copilot.Contracts;
using Elsa.Copilot.Implementations;
using Elsa.Copilot.Options;
using Elsa.Copilot.Tasks;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

#pragma warning disable SKEXP0110

namespace Elsa.Copilot.Features;

public class CopilotFeature(IModule module) : FeatureBase(module)
{
    private Action<CopilotOptions> _configureOptions = _ => { };
    private Action<IServiceProvider, IKernelBuilder>? _configureSk;

    public CopilotFeature ConfigureOptions(Action<CopilotOptions> configure)
    {
        _configureOptions += configure;
        return this;
    }
    
    public CopilotFeature ConfigureSk(Action<IServiceProvider, IKernelBuilder> configure)
    {
        _configureSk += configure;
        return this;
    }

    public override void Apply()
    {
        Services.Configure(_configureOptions);
        Services.AddSingleton<IAgentTemplateLoader, AgentTemplateLoader>();
        Services.AddSingleton<IAgentGroupChatFactory, AgentGroupChatFactory>();

        Services.AddSingleton<Kernel>(sp =>
        {
            var builder = Kernel.CreateBuilder();
            _configureSk?.Invoke(sp, builder);

            // Register Copilot tools
            var toolPlugin = new CopilotToolPlugin(sp);
            builder.Plugins.AddFromObject(toolPlugin);

            return builder.Build();
        });

        Services.AddStartupTask<LoadAgentsStartupTask>();
    }
}