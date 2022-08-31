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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Cluster;
using Proto.DependencyInjection;
using Proto.Remote;
using Proto.Remote.GrpcNet;

namespace Elsa.ProtoActor.Configuration;

[DependsOn(typeof(WorkflowRuntimeFeature))]
public class ProtoActorFeature : FeatureBase
{
    public ProtoActorFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        // Configure runtime with ProtoActor workflow invoker.
        Module.Configure<WorkflowRuntimeFeature>().WorkflowRuntime = sp => ActivatorUtilities.CreateInstance<ProtoActorWorkflowRuntime>(sp);
    }

    public ProtoActorFeature ConfigureProtoActorBuilder(Func<IServiceProvider, ProtoActorSystem> factory)
    {
        ProtoActorBuilderFactory = factory;
        return this;
    }

    // Configure a default proto actor system using the localhost provider.
    public Func<IServiceProvider, ProtoActorSystem> ProtoActorBuilderFactory { get; set; } = _ => new ProtoActorBuilder().UseLocalhostProvider("elsa-cluster", true).Build();

    public override void ConfigureHostedServices()
    {
        Services.AddHostedService<WorkflowServerHost>();
    }
    
    public override void Apply()
    {
        var services = Services;
        services.AddSingleton(ProtoActorBuilderFactory);

        // Logging.
        Log.SetLoggerFactory(LoggerFactory.Create(l => l.AddConsole().SetMinimumLevel(LogLevel.Warning)));

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
            .AddSingleton(sp => new WorkflowGrainActor((context, _) => ActivatorUtilities.CreateInstance<WorkflowGrain>(sp, context)))
            .AddSingleton(sp => new BookmarkGrainActor((context, _) => ActivatorUtilities.CreateInstance<BookmarkGrain>(sp, context)))
            ;

        // Client factory.
        services.AddSingleton<GrainClientFactory>();
    }
}