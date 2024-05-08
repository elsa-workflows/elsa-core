using Elsa.Caching.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Runtime.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Features;

/// <summary>
/// Installs and configures workflow runtime caching features.
/// </summary>
[DependsOn(typeof(MemoryCacheFeature))]
public class CachingWorkflowRuntimeFeature : FeatureBase
{
    /// <inheritdoc />
    public CachingWorkflowRuntimeFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
        // Decorators.
        Services.Decorate<ITriggerStore, CachingTriggerStore>();
    }
}