using Elsa.Actors.ProtoActor.Features;
using Elsa.Actors.ProtoActor.PubSub.Redis.Options;
using Elsa.Actors.ProtoActor.PubSub.Redis.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Elsa.Actors.ProtoActor.PubSub.Redis.Features;

/// <summary>
/// Configures the core Proto.Actor feature to store Pub/Sub subscribers in Redis.
/// </summary>
/// <remarks>
/// Every cluster member must use the same Redis database and deterministic key configuration.
/// </remarks>
[DependsOn(typeof(ProtoActorFeature))]
public class RedisPubSubSubscribersStoreFeature(IModule module) : FeatureBase(module)
{
    /// <summary>
    /// Gets or sets the delegate that resolves the shared Redis database.
    /// </summary>
    /// <remarks>
    /// The default delegate resolves <see cref="IConnectionMultiplexer"/> from dependency injection and uses its default
    /// database. Register an <see cref="IConnectionMultiplexer"/> or provide a custom delegate before applying the module.
    /// </remarks>
    public Func<IServiceProvider, IDatabase> CreateDatabase { get; set; } =
        sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase();

    /// <summary>
    /// Gets or sets the delegate that configures Redis key generation.
    /// </summary>
    public Action<RedisSubscribersStoreOptions> ConfigureOptions { get; set; } = _ => { };

    /// <inheritdoc />
    public override void Configure()
    {
        var options = new RedisSubscribersStoreOptions();
        ConfigureOptions(options);

        Module.Configure<ProtoActorFeature>(feature =>
            feature.CreatePubSubSubscribersStore = sp =>
                new RedisSubscribersStore(feature.ClusterName, CreateDatabase(sp), options));
    }
}
