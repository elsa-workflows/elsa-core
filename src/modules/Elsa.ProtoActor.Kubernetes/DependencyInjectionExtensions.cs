using System;
using Elsa.ProtoActor.Common;
using Elsa.ProtoActor.Configuration;

namespace Elsa.ProtoActor.Kubernetes;

public static class DependencyInjectionExtensions
{
    public static ProtoActorFeature WithKubernetesProvider(this ProtoActorFeature protoActorFeature, Action<KubernetesProviderOptions> providerOptions)
    {
        var options = new KubernetesProviderOptions();
        providerOptions?.Invoke(options);
        
        protoActorFeature.ConfigureProtoActorBuilder(sp =>
            new ProtoActorBuilder().UseKubernetesProvider(options).Build());
        return protoActorFeature;
    }
}