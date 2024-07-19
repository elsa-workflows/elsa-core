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
using Proto.OpenTelemetry;
using Proto.Persistence;
using Proto.Remote;
using Proto.Remote.GrpcNet;

namespace Elsa.ProtoActor.Features;

/// Installs the Proto Actor feature to host &amp; execute workflow instances.
[DependsOn(typeof(WorkflowsFeature))]
[DependsOn(typeof(WorkflowRuntimeFeature))]
public class ProtoActorFeature : FeatureBase
{
    private string _clusterName = "elsa-cluster";
    private LogLevel _diagnosticsLogLevel = LogLevel.Information;
    private bool _enableMetrics;
    private bool _enableTracing;

    /// <inheritdoc />
    public ProtoActorFeature(IModule module) : base(module)
    {
    }

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
        // Configure runtime with ProtoActor workflow runtime.
        Module.Configure<WorkflowRuntimeFeature>().WorkflowRuntime = sp => ActivatorUtilities.CreateInstance<ProtoActorRuntime>(sp);
    }

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
            var workflowGrainProps = system.DI().PropsFor<WorkflowInstanceActor>();

            // if (_enableTracing)
            //     workflowGrainProps = workflowGrainProps.WithTracing();

            var clusterConfig = ClusterConfig
                    .Setup(_clusterName, clusterProvider, new PartitionIdentityLookup())
                    .WithHeartbeatExpiration(TimeSpan.FromDays(1))
                    .WithActorRequestTimeout(TimeSpan.FromSeconds(1000))
                    .WithActorSpawnVerificationTimeout(TimeSpan.FromHours(1))
                    .WithActorActivationTimeout(TimeSpan.FromHours(1))
                    .WithActorSpawnVerificationTimeout(TimeSpan.FromHours(1))
                    .WithGossipRequestTimeout(TimeSpan.FromHours(1))
                    .WithClusterKind(WorkflowInstanceActor.Kind, workflowGrainProps)
                ;

            ConfigureClusterConfig?.Invoke(sp, clusterConfig);
            var remoteConfig = ConfigureRemoteConfig(sp);

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