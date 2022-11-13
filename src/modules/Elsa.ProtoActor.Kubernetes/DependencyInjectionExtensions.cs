using Elsa.Features.Extensions;
using Elsa.ProtoActor.Features;

namespace Elsa.ProtoActor.Kubernetes;

public static class DependencyInjectionExtensions
{
    public static ProtoActorFeature UseKubernetes(this ProtoActorFeature protoActorFeature, Action<KubernetesProtoActorFeature>? kubernetesFeature = default)
    {
        protoActorFeature.Module.Use<KubernetesProtoActorFeature>(feature => kubernetesFeature?.Invoke(feature));
        return protoActorFeature;
    }
}