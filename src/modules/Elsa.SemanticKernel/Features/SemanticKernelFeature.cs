using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.SemanticKernel.ActivityProviders;
using Elsa.Workflows.Management.Features;
using Elsa.SemanticKernel.HostedServices;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.SemanticKernel.Features;

/// A feature that installs Semantic Kernel functionality.
[DependsOn(typeof(WorkflowManagementFeature))]
[UsedImplicitly]
public class SemanticKernelFeature(IModule module) : FeatureBase(module)
{
    public override void ConfigureHostedServices()
    {
        Module.ConfigureHostedService<ConfigureSemanticKernel>(-2);
        Module.ConfigureHostedService<ConfigureAgentManager>(-1);
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddActivityProvider<AgentActivityProvider>()
            .AddSemanticKernel()
            ;
    }
}