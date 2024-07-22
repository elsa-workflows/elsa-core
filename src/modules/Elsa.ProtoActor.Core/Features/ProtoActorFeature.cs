using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.ProtoActor.HostedServices;
using Elsa.Workflows.Runtime.ProtoActor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Partition;
using Proto.Cluster.Testing;
using Proto.DependencyInjection;
using Proto.OpenTelemetry;
using Proto.Persistence;
using Proto.Remote;
using Proto.Remote.GrpcNet;

namespace Elsa.ProtoActor.Features;

/// Installs the Proto Actor feature.
public class ProtoActorFeature(IModule module) : FeatureBase(module)
{
    private string _clusterName = "elsa-cluster";
    private LogLevel _diagnosticsLogLevel = LogLevel.Information;
    private bool _enableMetrics;
    private bool _enableTracing;

    /// A delegate that returns an instance of a concrete implementation of <see cref="IClusterProvider"/>. 
    public Func<IServiceProvider, IClusterProvider> CreateClusterProvider { get; set; } = _ => new TestProvider(new TestProviderOptions(), new InMemAgent());

    /// A delegate that configures an instance of <see cref="ConfigureActorSystemConfig"/>. 
    public Action<IServiceProvider, ActorSystemConfig> ConfigureActorSystemConfig { get; set; } = SetupDefaultConfig;

    /// A delegate that configures an instance of an <see cref="ConfigureActorSystem"/>. 
    public Action<IServiceProvider, ActorSystem> ConfigureActorSystem { get; set; } = (_, _) => { };

    /// A delegate that returns an instance of <see cref="GrpcNetRemoteConfig"/> to be used by the actor system. 
    public Func<IServiceProvider, GrpcNetRemoteConfig> ConfigureRemoteConfig { get; set; } = CreateDefaultRemoteConfig;

    /// A delegate that returns an instance of a concrete implementation of <see cref="IProvider"/> to use for persisting events and snapshots. 
    public Func<IServiceProvider, IProvider> PersistenceProvider { get; set; } = _ => new InMemoryProvider();

    /// A delegate that configures an instance of <see cref="ClusterConfig"/>. 
    public Action<IServiceProvider, ClusterConfig>? ConfigureClusterConfig { get; set; }

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
        Module.ConfigureHostedService<StartClusterMember>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        var services = Services;

        // Register ActorSystem.
        services.AddSingleton(sp =>
        {
            var actorSystemConfig = ActorSystemConfig
                .Setup()
                .WithDiagnosticsLogLevel(_diagnosticsLogLevel);

            if (_enableMetrics)
                actorSystemConfig = actorSystemConfig.WithMetrics();

            if (_enableTracing)
                actorSystemConfig = actorSystemConfig.WithConfigureProps(props => props.WithTracing());

            ConfigureActorSystemConfig(sp, actorSystemConfig);

            var clusterProvider = CreateClusterProvider(sp);
            var system = new ActorSystem(actorSystemConfig).WithServiceProvider(sp);
            var clusterConfig = ClusterConfig
                .Setup(_clusterName, clusterProvider, new PartitionIdentityLookup())
                .WithHeartbeatExpiration(TimeSpan.FromDays(1))
                .WithActorRequestTimeout(TimeSpan.FromSeconds(1000))
                .WithActorSpawnVerificationTimeout(TimeSpan.FromHours(1))
                .WithActorActivationTimeout(TimeSpan.FromHours(1))
                .WithActorSpawnVerificationTimeout(TimeSpan.FromHours(1))
                .WithGossipRequestTimeout(TimeSpan.FromHours(1));
            
            var virtualActorProviders = sp.GetServices<IVirtualActorsProvider>().ToList();

            var remoteConfig = ConfigureRemoteConfig(sp);
            
            foreach (var virtualActorProvider in virtualActorProviders)
            {
                var clusterKinds = virtualActorProvider.GetClusterKinds(system).ToList();

                foreach (var clusterKind in clusterKinds)
                {
                    var kind = clusterKind;
                    if (_enableTracing)
                        kind = kind.WithProps(props => props.WithTracing());

                    clusterConfig.WithClusterKind(kind);
                }

                var messageDescriptors = virtualActorProvider.GetFileDescriptors().ToArray();
                remoteConfig.WithProtoMessages(messageDescriptors);
            }

            ConfigureClusterConfig?.Invoke(sp, clusterConfig);
            
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

    private static void SetupDefaultConfig(IServiceProvider serviceProvider, ActorSystemConfig config)
    {
    }

    private static GrpcNetRemoteConfig CreateDefaultRemoteConfig(IServiceProvider serviceProvider)
    {
        return GrpcNetRemoteConfig.BindToLocalhost();
    }
}