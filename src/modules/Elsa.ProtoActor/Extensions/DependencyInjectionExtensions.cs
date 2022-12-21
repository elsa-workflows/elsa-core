using Elsa.ProtoActor.Features;
using Elsa.Workflows.Runtime.Features;

namespace Elsa.ProtoActor.Extensions;

/// <summary>
/// Provides extension methods on <see cref="WorkflowRuntimeFeature"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Enable & configure the <see cref="WorkflowRuntimeFeature"/>.
    /// </summary>
    public static WorkflowRuntimeFeature UseProtoActor(this WorkflowRuntimeFeature feature, Action<ProtoActorFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}