using Elsa.Caching.Distributed.Features;
using Elsa.Caching.Distributed.ProtoActor.Actors;
using Elsa.Caching.Distributed.ProtoActor.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Caching.Distributed.ProtoActor.Features;

/// Installs the Proto Actor feature to host &amp; execute workflow instances.
[DependsOn(typeof(DistributedCacheFeature))]
public class ProtoActorDistributedCacheFeature : FeatureBase
{
    /// <inheritdoc />
    public ProtoActorDistributedCacheFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        // Configure runtime with ProtoActor workflow runtime.
        Module.Configure<DistributedCacheFeature>().WithChangeTokenSignalPublisher(sp => ActivatorUtilities.CreateInstance<ProtoActorChangeTokenSignalPublisher>(sp));
    }

    /// <inheritdoc />
    public override void Apply()
    {
        var services = Services;

        // Actors.
        services
            .AddTransient(sp => new LocalCacheActor((context, _) => ActivatorUtilities.CreateInstance<LocalCacheImpl>(sp, context)));

        // Distributed runtime.
        services.AddSingleton<ProtoActorChangeTokenSignalPublisher>();
    }
}