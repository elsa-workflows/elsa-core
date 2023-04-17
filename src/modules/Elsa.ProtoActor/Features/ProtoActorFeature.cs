using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.ProtoActor.Grains;
using Elsa.ProtoActor.HostedServices;
using Elsa.ProtoActor.Protos;
using Elsa.ProtoActor.Services;
using Elsa.Workflows.Core.Features;
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
        // Configure default workflow execution pipeline suitable for Proto Actor.
        Module.UseWorkflows(workflows => workflows.WithProtoActorRuntimeWorkflowExecutionPipeline());
            
        // Configure runtime with ProtoActor workflow runtime.
        Module.Configure<WorkflowRuntimeFeature>().WorkflowRuntime = sp => ActivatorUtilities.CreateInstance<ProtoActorWorkflowRuntime>(sp);
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
        Module.ConfigureHostedService<WorkflowServerHost>();
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
                .WithMetrics();
            
            var clusterProvider = ClusterProvider(sp);
            var system = new ActorSystem(systemConfig).WithServiceProvider(sp);
            var workflowGrainProps = system.DI().PropsFor<WorkflowGrainActor>();
            var workflowRegistryGrainProps = system.DI().PropsFor<RunningWorkflowsGrainActor>();
            
            var clusterConfig = ClusterConfig
                    .Setup(ClusterName, clusterProvider, new PartitionIdentityLookup())
                    .WithHeartbeatExpiration(TimeSpan.FromDays(1))
                    .WithActorRequestTimeout(TimeSpan.FromHours(1))
                    .WithActorActivationTimeout(TimeSpan.FromHours(1))
                    .WithActorSpawnVerificationTimeout(TimeSpan.FromHours(1))
                    .WithClusterKind(WorkflowGrainActor.Kind, workflowGrainProps)
                    .WithClusterKind(RunningWorkflowsGrainActor.Kind, workflowRegistryGrainProps)
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
        services.AddSingleton(PersistenceProvider);
        
        // Mediator handlers.
        services.AddHandlersFrom<ProtoActorFeature>();

        // Cluster.
        services.AddSingleton(sp => sp.GetRequiredService<ActorSystem>().Cluster());

        // Actors.
        services
            .AddTransient(sp => new WorkflowGrainActor((context, _) => ActivatorUtilities.CreateInstance<WorkflowGrain>(sp, context)))
            .AddTransient(sp => new RunningWorkflowsGrainActor((context, _) => ActivatorUtilities.CreateInstance<RunningWorkflowsGrain>(sp, context)))
            ;
    }
    
    private static void SetupDefaultConfig(IServiceProvider serviceProvider, ActorSystemConfig config)
    {
    }
    
    private static GrpcNetRemoteConfig CreateDefaultRemoteConfig(IServiceProvider serviceProvider) =>
        GrpcNetRemoteConfig.BindToLocalhost()
            .WithProtoMessages(MessagesReflection.Descriptor)
            .WithProtoMessages(EmptyReflection.Descriptor);
}