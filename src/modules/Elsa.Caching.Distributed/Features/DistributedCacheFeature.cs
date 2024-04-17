using Elsa.Caching.Distributed.Contracts;
using Elsa.Caching.Distributed.Services;
using Elsa.Caching.Features;
using Elsa.Caching.Options;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Caching.Distributed.Features;

/// <summary>
/// Configures distributed cache management.
/// </summary>
[DependsOn(typeof(MemoryCacheFeature))]
public class DistributedCacheFeature(IModule module) : FeatureBase(module)
{
    /// <summary>
    /// A delegate to configure the <see cref="CachingOptions"/>.
    /// </summary>
    private Func<IServiceProvider, IChangeTokenSignalPublisher> ChangeTokenSignalPublisherFactory { get; set; } = _ => new NoopChangeTokenSignalPublisher();

    /// <summary>
    /// Configures the change token signal publisher.
    /// </summary>
    public DistributedCacheFeature WithChangeTokenSignalPublisher(Func<IServiceProvider, IChangeTokenSignalPublisher> factory)
    {
        ChangeTokenSignalPublisherFactory = factory;
        return this;
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton(ChangeTokenSignalPublisherFactory);
        Services.Decorate<IChangeTokenSignaler, DistributedChangeTokenSignaler>();
        Services.AddSingleton<IChangeTokenSignalInvoker>(sp => (DistributedChangeTokenSignaler)sp.GetRequiredService<IChangeTokenSignaler>());
    }
}