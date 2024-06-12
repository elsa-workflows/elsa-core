using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.ProtoActor.Actors;
using Elsa.ProtoActor.HostedServices;
using Elsa.ProtoActor.Mappers;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.ProtoActor.Services;
using Elsa.Workflows.Features;
using Elsa.Workflows.Runtime.Features;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Partition;
using Proto.Cluster.Testing;
using Proto.DependencyInjection;
using Proto.Persistence;
using Proto.Remote;
using Proto.Remote.GrpcNet;

namespace Elsa.ProtoActor.Features;

/// <summary>
/// Installs the Proto Actor feature to host &amp; execute workflow instances.
/// </summary>
[DependsOn(typeof(WorkflowsFeature))]
[DependsOn(typeof(WorkflowRuntimeFeature))]
public class ProtoActorFeature : FeatureBase
{
    /// <inheritdoc />
    public ProtoActorFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        // Configure runtime with ProtoActor workflow runtime.
        Module.Configure<WorkflowRuntimeFeature>().WorkflowRuntime = sp => ActivatorUtilities.CreateInstance<ProtoActorRuntime>(sp);
    }

    /// <summary>
    /// The name of the cluster to configure.
    /// </summary>
    public string ClusterName { get; set; } = "elsa-cluster";

    /// <summary>
    /// A delegate that returns an instance of a concrete implementation of <see cref="IClusterProvider"/>. 
    /// </summary>
    public Func<IServiceProvider, IClusterProvider> ClusterProvider { get; set; } = _ => new TestProvider(new TestProviderOptions(), new InMemAgent());

    /// <summary>
    /// A delegate that configures an instance of <see cref="ActorSystemConfig"/>. 
    /// </summary>
    public Action<IServiceProvider, ActorSystemConfig> ActorSystemConfig { get; set; } = SetupDefaultConfig;

    /// <summary>
    /// A delegate that configures an instance of an <see cref="ActorSystem"/>. 
    /// </summary>
    public Action<IServiceProvider, ActorSystem> ActorSystem { get; set; } = (_, _) => { };

    /// <summary>
    /// A delegate that returns an instance of <see cref="GrpcNetRemoteConfig"/> to be used by the actor system. 
    /// </summary>
    public Func<IServiceProvider, GrpcNetRemoteConfig> RemoteConfig { get; set; } = CreateDefaultRemoteConfig;

    /// <summary>
    /// A delegate that returns an instance of a concrete implementation of <see cref="IProvider"/> to use for persisting events and snapshots. 
    /// </summary>
    public Func<IServiceProvider, IProvider> PersistenceProvider { get; set; } = _ => new InMemoryProvider();

    /// <inheritdoc />
    public override void ConfigureHostedServices()
    {
        Module.ConfigureHostedService<WorkflowSystemHost>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        var services = Services;

        // Register ActorSystem.
        services.AddSingleton(sp =>
        {
            var systemConfig = Proto.ActorSystemConfig
                .Setup()
                .WithDiagnosticsLogLevel(LogLevel.Debug)
                .WithMetrics();

            var clusterProvider = ClusterProvider(sp);
            var system = new ActorSystem(systemConfig).WithServiceProvider(sp);
            var workflowGrainProps = system.DI().PropsFor<WorkflowInstanceActor>();

            var clusterConfig = ClusterConfig
                    .Setup(ClusterName, clusterProvider, new PartitionIdentityLookup())
                    .WithHeartbeatExpiration(TimeSpan.FromDays(1))
                    .WithActorRequestTimeout(TimeSpan.FromSeconds(1000))
                    .WithActorSpawnVerificationTimeout(TimeSpan.FromHours(1))
                    .WithActorActivationTimeout(TimeSpan.FromHours(1))
                    .WithActorSpawnVerificationTimeout(TimeSpan.FromHours(1))
                    .WithGossipRequestTimeout(TimeSpan.FromHours(1))
                    //.WithLegacyRequestTimeoutBehavior()
                    .WithClusterKind(WorkflowInstanceActor.Kind, workflowGrainProps)
                ;

            ActorSystemConfig(sp, systemConfig);

            var remoteConfig = RemoteConfig(sp);

            system
                .WithRemote(remoteConfig)
                .WithCluster(clusterConfig);

            ActorSystem(sp, system);
            return system;
        });

        // Logging.
        Log.SetLoggerFactory(LoggerFactory.Create(l => l.AddConsole().SetMinimumLevel(LogLevel.Warning)));

        // Persistence.
        services.AddTransient(PersistenceProvider);

        // Mappers.
        services
            .AddSingleton<Mappers.Mappers>()
            .AddSingleton<ActivityHandleMapper>()
            .AddSingleton<WorkflowDefinitionHandleMapper>()
            .AddSingleton<ActivityIncidentMapper>()
            .AddSingleton<ExceptionMapper>()
            .AddSingleton<ActivityIncidentStateMapper>()
            .AddSingleton<WorkflowStatusMapper>()
            .AddSingleton<WorkflowSubStatusMapper>()
            .AddSingleton<CreateWorkflowInstanceRequestMapper>()
            .AddSingleton<CreateWorkflowInstanceResponseMapper>()
            .AddSingleton<RunWorkflowInstanceRequestMapper>()
            .AddSingleton<RunWorkflowInstanceResponseMapper>()
            .AddSingleton<CreateAndRunWorkflowInstanceRequestMapper>()
            .AddSingleton<RunWorkflowParamsMapper>()
            .AddSingleton<WorkflowStateJsonMapper>();

        // Mediator handlers.
        services.AddHandlersFrom<ProtoActorFeature>();

        // Cluster.
        services.AddSingleton(sp => sp.GetRequiredService<ActorSystem>().Cluster());

        // Actors.
        services
            .AddTransient(sp => new WorkflowInstanceActor((context, _) => ActivatorUtilities.CreateInstance<WorkflowInstanceImpl>(sp, context)));
            
        // Distributed runtime.
        services.AddSingleton<ProtoActorRuntime>();
    }

    private static void SetupDefaultConfig(IServiceProvider serviceProvider, ActorSystemConfig config)
    {
    }

    private static GrpcNetRemoteConfig CreateDefaultRemoteConfig(IServiceProvider serviceProvider) =>
        GrpcNetRemoteConfig.BindToLocalhost()
            .WithProtoMessages(SharedReflection.Descriptor)
            .WithProtoMessages(EmptyReflection.Descriptor);
}