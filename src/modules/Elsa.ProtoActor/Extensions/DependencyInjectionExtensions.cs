using System;
using Elsa.ProtoActor.Configuration;
using Elsa.Workflows.Runtime.Features;

namespace Elsa.ProtoActor.Extensions;

public static class DependencyInjectionExtensions
{
    public static WorkflowRuntimeFeature UseProtoActor(this WorkflowRuntimeFeature feature, Action<ProtoActorFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}