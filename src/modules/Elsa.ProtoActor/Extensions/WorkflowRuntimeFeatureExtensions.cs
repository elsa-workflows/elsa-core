using Elsa.ProtoActor.Features;
using Elsa.Workflows.Runtime.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods on <see cref="WorkflowRuntimeFeature"/>.
/// </summary>
public static class WorkflowRuntimeFeatureExtensions
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