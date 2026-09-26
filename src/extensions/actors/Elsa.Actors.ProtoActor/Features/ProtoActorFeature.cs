using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Actors.ProtoActor.HostedServices;
using Elsa.Actors.ProtoActor.Middleware;
using Elsa.Actors.ProtoActor.Services;
using Elsa.Workflows.Runtime.ProtoActor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Partition;
using Proto.Cluster.PubSub;
using Proto.Cluster.Testing;
using Proto.DependencyInjection;
using Proto.OpenTelemetry;
using Proto.Persistence;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using Proto.Utils;

namespace Elsa.Actors.ProtoActor.Features;

/// <summary>
/// Installs the Proto Actor feature.
/// </summary>
public class ProtoActorFeature(IModule module) : FeatureBase(module)
{
    private LogLevel _diagnosticsLogLevel = LogLevel.Information;
    private bool _enableMetrics;
    private bool _enableTracing;

    /// <summary>
    /// Gets or sets the name of the cluster.
    /// </summary>
    /// <remarks>
    /// The ClusterName property specifies the name of the cluster that will be used by the Proto Actor feature.
    /// By default, the cluster name is set to "elsa-cluster".
    /// </remarks>
    public string ClusterName { get; set; } = "elsa-cluster";

    /// <summary>
    /// A delegate that returns an instance of a concrete implementation of <see cref="IClusterProvider"/>.
    /// </summary>
    public Func<IServiceProvider, IClusterProvider> CreateClusterProvider { get; set; } = _ => new TestProvider(new TestProviderOptions(), new InMemAgent());

    /// <summary>
    /// A delegate that configures an instance of <see cref="ConfigureActorSystemConfig"/>.
    /// </summary>
    public Func<IServiceProvider, ActorSystemConfig, ActorSystemConfig> ConfigureActorSystemConfig { get; set; } = SetupDefaultConfig;

    /// <summary>
    /// A delegate that configures an instance of an <see cref="ConfigureActorSystem"/>.
    /// </summary>
    public Action<IServiceProvider, ActorSystem> ConfigureActorSystem { get; set; } = (_, _) => { };

    /// <summary>
    /// A delegate that returns an instance of <see cref="GrpcNetRemoteConfig"/> to be used by the actor system.
    /// </summary>
    public Func<IServiceProvider, RemoteConfig> ConfigureRemoteConfig { get; set; } = CreateDefaultRemoteConfig;

    /// <summary>
    /// A delegate that returns an instance of a concrete implementation of <see cref="IProvider"/> to use for persisting events and snapshots.
    /// </summary>
    public Func<IServiceProvider, IProvider> PersistenceProvider { get; set; } = _ => new InMemoryProvider();

    /// <summary>
    /// A delegate that configures an instance of <see cref="ClusterConfig"/>.
    /// </summary>
    public Func<IServiceProvider, ClusterConfig, ClusterConfig>? ConfigureClusterConfig { get; set; }

    /// <summary>
    /// A delegate that creates the key-value store used by Proto.Actor Pub/Sub to persist topic subscribers.
    /// </summary>
    /// <remarks>
    /// The default store survives topic reactivation only within the same process and member. It does not survive
    /// process restarts or migration. Clustered applications should replace it with shared, durable storage such as Redis.
    /// </remarks>
    public Func<IServiceProvider, IKeyValueStore<Subscribers>> CreatePubSubSubscribersStore { get; set; } = _ => new InMemorySubscribersStore();

    public ProtoActorFeature EnableMetrics(bool value = true)
    {
        _enableMetrics = value;
        return this;
    }

    public ProtoActorFeature EnableTracing(bool value = true)
    {
        _enableTracing = value;
        return this;
    }

    public ProtoActorFeature WithDiagnosticsLevel(LogLevel value)
    {
        _diagnosticsLogLevel = value;
        return this;
    }

    /// <inheritdoc />
    public override void Configure()
    {
    }

    /// <inheritdoc />
    public override void ConfigureHostedServices()
    {
        Module.ConfigureHostedService<StartClusterMember>(-5);
    }

    /// <inheritdoc />
    public override void Apply()
    {
        var services = Services;

        services.TryAddSingleton<IKeyValueStore<Subscribers>>(sp => CreatePubSubSubscribersStore(sp));

        // Register ActorSystem.
        services.AddSingleton(sp =>
        {
            var actorSystemConfig = ActorSystemConfig
                .Setup()
                .WithDiagnosticsLogLevel(_diagnosticsLogLevel);

            if (_enableMetrics)
                actorSystemConfig = actorSystemConfig.WithMetrics();

            actorSystemConfig = actorSystemConfig.WithConfigureProps(props =>
            {
                if (_enableTracing)
                    props = props.WithTracing();

                return props;
            });

            ConfigureActorSystemConfig(sp, actorSystemConfig);

            var clusterProvider = CreateClusterProvider(sp);
            var system = new ActorSystem(actorSystemConfig).WithServiceProvider(sp);
            var clusterConfig = ClusterConfig
                .Setup(ClusterName, clusterProvider, new PartitionIdentityLookup())
                .WithHeartbeatExpiration(TimeSpan.FromDays(1))
                .WithActorRequestTimeout(TimeSpan.FromSeconds(1000))
                .WithActorSpawnVerificationTimeout(TimeSpan.FromHours(1))
                .WithActorActivationTimeout(TimeSpan.FromHours(1))
                .WithGossipRequestTimeout(TimeSpan.FromHours(1));

            var remoteConfig = ConfigureRemoteConfig(sp);
            (clusterConfig, remoteConfig) = AddVirtualActors(sp, system, clusterConfig, remoteConfig);

            if (ConfigureClusterConfig != null)
                clusterConfig = ConfigureClusterConfig(sp, clusterConfig);

            if (clusterConfig.ClusterKinds.All(x => x.Name != TopicActor.Kind))
            {
                var topicActorProps = Props.FromProducer(() => new TopicActor(sp.GetRequiredService<IKeyValueStore<Subscribers>>()));
                clusterConfig = clusterConfig.WithClusterKind(TopicActor.Kind, topicActorProps);
            }

            system
                .WithRemote(remoteConfig)
                .WithCluster(clusterConfig);

            ConfigureActorSystem(sp, system);

            return system;
        });

        // Logging.
        Log.SetLoggerFactory(LoggerFactory.Create(l => l.AddConsole().SetMinimumLevel(LogLevel.Warning)));

        // Persistence.
        services.AddTransient(PersistenceProvider);

        // Cluster.
        services.AddSingleton(sp => sp.GetRequiredService<ActorSystem>().Cluster());
    }

    private (ClusterConfig ClusterConfig, RemoteConfig RemoteConfig) AddVirtualActors(
        IServiceProvider sp,
        ActorSystem system,
        ClusterConfig clusterConfig,
        RemoteConfig remoteConfig)
    {
        var virtualActorProviders = sp.GetServices<IVirtualActorsProvider>().ToList();

        foreach (var virtualActorProvider in virtualActorProviders)
        {
            var clusterKinds = virtualActorProvider.GetClusterKinds(system).ToList();

            foreach (var clusterKind in clusterKinds)
            {
                var kind = clusterKind;
                if (_enableTracing)
                    kind = kind.WithProps(props => props.WithTracing());

                kind = kind.WithProps(props => props.WithMultitenancy(sp));
                clusterConfig = clusterConfig.WithClusterKind(kind);
            }

            var messageDescriptors = virtualActorProvider.GetFileDescriptors().ToArray();
            remoteConfig = remoteConfig.WithProtoMessages(messageDescriptors);
        }

        return (clusterConfig, remoteConfig);
    }

    private static ActorSystemConfig SetupDefaultConfig(IServiceProvider serviceProvider, ActorSystemConfig config)
    {
        return config;
    }

    private static RemoteConfig CreateDefaultRemoteConfig(IServiceProvider serviceProvider)
    {
        return RemoteConfig.BindToLocalhost();
    }
}
