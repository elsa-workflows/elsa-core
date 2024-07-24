using Elsa.Caching.Distributed.Features;
using Elsa.Caching.Distributed.ProtoActor.Actors;
using Elsa.Caching.Distributed.ProtoActor.HostedServices;
using Elsa.Caching.Distributed.ProtoActor.ProtoBuf;
using Elsa.Caching.Distributed.ProtoActor.Providers;
using Elsa.Caching.Distributed.ProtoActor.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.ProtoActor;
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

    public override void ConfigureHostedServices()
    {
        ConfigureHostedService<StartLocalCacheActor>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        var services = Services;
        
        // Actor providers.
        services.AddSingleton<IVirtualActorsProvider, LocalCacheVirtualActorProvider>();

        // Actors.
        services
            .AddTransient(sp => new LocalCacheActor((context, _) => ActivatorUtilities.CreateInstance<LocalCache>(sp, context)));

        // Distributed runtime.
        services.AddSingleton<ProtoActorChangeTokenSignalPublisher>();
    }
}