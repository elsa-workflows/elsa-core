using Elsa.Common.Multitenancy;
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

/// <summary>
/// Installs the Proto Actor feature.
/// </summary>
public class ProtoActorFeature(IModule module) : FeatureBase(module)
{
    private const string TenantHeaderName = "TenantId";
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
    public Func<IServiceProvider, GrpcNetRemoteConfig> ConfigureRemoteConfig { get; set; } = CreateDefaultRemoteConfig;

    /// <summary>
    /// A delegate that returns an instance of a concrete implementation of <see cref="IProvider"/> to use for persisting events and snapshots.
    /// </summary>
    public Func<IServiceProvider, IProvider> PersistenceProvider { get; set; } = _ => new InMemoryProvider();

    /// <summary>
    /// A delegate that configures an instance of <see cref="ClusterConfig"/>.
    /// </summary>
    public Func<IServiceProvider, ClusterConfig, ClusterConfig>? ConfigureClusterConfig { get; set; }

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
            clusterConfig = AddVirtualActors(sp, system, clusterConfig);

            if (ConfigureClusterConfig != null)
                clusterConfig = ConfigureClusterConfig(sp, clusterConfig);

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

    private ClusterConfig AddVirtualActors(IServiceProvider sp, ActorSystem system, ClusterConfig clusterConfig)
    {
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

                kind = kind.WithProps(props =>
                {
                    props = props.WithReceiverMiddleware(next => async (context, envelope) =>
                    {
                        var tenantId = envelope.Header.GetValueOrDefault(TenantHeaderName);
                        if (tenantId != null)
                        {
                            var tenantContextInitializer = sp.GetRequiredService<ITenantContextInitializer>();
                            await tenantContextInitializer.InitializeAsync(tenantId);
                        }

                        await next(context, envelope);
                    }).WithSenderMiddleware(next => async (context, target, envelope) =>
                    {
                        var tenantAccessor = sp.GetRequiredService<ITenantAccessor>();
                        if (tenantAccessor.Tenant != null) envelope.WithHeader(TenantHeaderName, tenantAccessor.Tenant.Id);
                        await next(context, target, envelope);
                    });

                    return props;
                });
                clusterConfig = clusterConfig.WithClusterKind(kind);
            }

            var messageDescriptors = virtualActorProvider.GetFileDescriptors().ToArray();
            remoteConfig = remoteConfig.WithProtoMessages(messageDescriptors);
        }

        return clusterConfig;
    }

    private static ActorSystemConfig SetupDefaultConfig(IServiceProvider serviceProvider, ActorSystemConfig config)
    {
        return config;
    }

    private static GrpcNetRemoteConfig CreateDefaultRemoteConfig(IServiceProvider serviceProvider)
    {
        return GrpcNetRemoteConfig.BindToLocalhost();
    }
}