using System;
using Elsa.ProtoActor.Common;
using Elsa.ProtoActor.Common.Options;
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

    public static ProtoActorFeature WithLocalhostProvider(this ProtoActorFeature protoActorFeature, Action<ProviderOptions>? providerOptions = null)
    {
        var options = new ProviderOptions();
        providerOptions?.Invoke(options);

        protoActorFeature.ConfigureProtoActorBuilder(sp =>
            new ProtoActorBuilder().UseLocalhostProvider(options.Name, options.WithDeveloperLogging).Build());
        return protoActorFeature;
    }
}
