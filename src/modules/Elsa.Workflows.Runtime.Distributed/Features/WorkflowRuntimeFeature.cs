using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Runtime.Distributed.Contracts;
using Elsa.Workflows.Runtime.Distributed.Handlers;
using Elsa.Workflows.Runtime.Distributed.Services;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Distributed.Features;

/// <summary>
/// Installs and configures workflow runtime features.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
public class DistributedRuntimeFeature : FeatureBase
{
    /// <inheritdoc />
    public DistributedRuntimeFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A factory that instantiates a concrete <see cref="IDistributedRuntime"/>.
    /// </summary>
    public Func<IServiceProvider, IDistributedRuntime> DistributedRuntime { get; set; } = sp => ActivatorUtilities.CreateInstance<LocalDistributedRuntime>(sp);

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton(DistributedRuntime);
        Services.AddCommandHandler<CancelWorkflowsCommandHandler>();
    }
}