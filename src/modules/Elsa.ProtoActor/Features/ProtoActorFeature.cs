using System;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.ProtoActor.Common;
using Elsa.ProtoActor.Extensions;
using Elsa.ProtoActor.Grains;
using Elsa.ProtoActor.HostedServices;
using Elsa.ProtoActor.Implementations;
using Elsa.Runtime.Protos;
using Elsa.Workflows.Runtime.Features;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Cluster;
using Proto.DependencyInjection;
using Proto.Persistence;
using Proto.Persistence.Sqlite;
using Proto.Remote;
using Proto.Remote.GrpcNet;

namespace Elsa.ProtoActor.Features;

[DependsOn(typeof(WorkflowRuntimeFeature))]
public class ProtoActorFeature : FeatureBase
{
    public ProtoActorFeature(IModule module) : base(module)
    {
        ConfigureActorSystem = (sp, config) =>
        {
            if (WithDeveloperLogging) config.WithDeveloperLogging();
            if (WithMetrics) config.WithMetrics();
        };

        ConfigureActorBuilder = (_, builder) => builder
            .WithClusterName(ClusterName)
            .UseLocalhostProvider(ClusterName, true)
            ;
    }

    public override void Configure()
    {
        // Configure runtime with ProtoActor workflow runtime.
        Module.Configure<WorkflowRuntimeFeature>().WorkflowRuntime = sp => ActivatorUtilities.CreateInstance<ProtoActorWorkflowRuntime>(sp);
    }

    public string ClusterName { get; set; } = "elsa-cluster";
    public bool WithDeveloperLogging { get; set; } = true;
    public bool WithMetrics { get; set; } = true;
    public Action<IServiceProvider, ActorSystemConfig> ConfigureActorSystem { get; set; }
    public Action<IServiceProvider, ProtoActorBuilder> ConfigureActorBuilder { get; set; }
    public Func<IServiceProvider, IProvider> PersistenceProvider { get; set; } = _ => new InMemoryProvider();

    public override void ConfigureHostedServices()
    {
        Services.AddHostedService<WorkflowServerHost>();
    }

    public override void Apply()
    {
        var services = Services;
        
        // Register ProtoActorSystem.
        services.AddSingleton(sp =>
        {
            var actorSystemConfig = ActorSystemConfig.Setup();
            var actorBuilder = new ProtoActorBuilder();

            ConfigureActorSystem(sp, actorSystemConfig);
            ConfigureActorBuilder(sp, actorBuilder);

            return actorBuilder.Build();
        });

        // Logging.
        Log.SetLoggerFactory(LoggerFactory.Create(l => l.AddConsole().SetMinimumLevel(LogLevel.Warning)));
        
        // Persistence.
        services.AddSingleton(PersistenceProvider);

        services.AddSingleton(sp =>
        {
            var protoActorSystem = sp.GetService<ProtoActorSystem>();
            var system = new ActorSystem(protoActorSystem!.ActorSystemConfig).WithServiceProvider(sp);

            var remoteConfig = protoActorSystem.RemoteConfig
                .WithProtoMessages(MessagesReflection.Descriptor)
                .WithProtoMessages(EmptyReflection.Descriptor);

            var workflowGrainProps = system.DI().PropsFor<WorkflowGrainActor>();
            var bookmarkGrainProps = system.DI().PropsFor<BookmarkGrainActor>();

            var clusterConfig =
                    ClusterConfig
                        .Setup(protoActorSystem.Name, protoActorSystem.ClusterProvider, protoActorSystem.IdentityLookup)
                        .WithHeartbeatExpiration(protoActorSystem.ClusterConfigurationSettings.HeartBeatExpiration)
                        .WithActorRequestTimeout(protoActorSystem.ClusterConfigurationSettings.ActorRequestTimeout)
                        .WithActorActivationTimeout(protoActorSystem.ClusterConfigurationSettings.ActorActivationTimeout)
                        .WithActorSpawnTimeout(protoActorSystem.ClusterConfigurationSettings.ActorSpawnTimeout)
                        .WithClusterKind(WorkflowGrainActor.Kind, workflowGrainProps)
                        .WithClusterKind(BookmarkGrainActor.Kind, bookmarkGrainProps)
                ;

            system
                .WithRemote(remoteConfig)
                .WithCluster(clusterConfig);

            return system;
        });

        // Cluster.
        services.AddSingleton(sp => sp.GetRequiredService<ActorSystem>().Cluster());

        // Actors.
        services
            .AddTransient(sp => new WorkflowGrainActor((context, _) => ActivatorUtilities.CreateInstance<WorkflowGrain>(sp, context)))
            .AddTransient(sp => new BookmarkGrainActor((context, _) => ActivatorUtilities.CreateInstance<BookmarkGrain>(sp, context)))
            ;
    }
}