using Elsa.Workflows.Runtime.ProtoActor.Features;
using Elsa.Workflows.Runtime.Features;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// Provides extension methods on <see cref="WorkflowRuntimeFeature"/>.
[PublicAPI]
public static class WorkflowRuntimeFeatureExtensions
{
    /// Enable &amp; configure the <see cref="WorkflowRuntimeFeature"/>.
    public static WorkflowRuntimeFeature UseProtoActor(this WorkflowRuntimeFeature feature, Action<ProtoActorWorkflowRuntimeFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}