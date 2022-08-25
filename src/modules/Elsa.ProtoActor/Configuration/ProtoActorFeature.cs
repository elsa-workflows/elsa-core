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
        Module.Configure<WorkflowRuntimeFeature>().WorkflowInvokerFactory =
            sp => ActivatorUtilities.CreateInstance<ProtoActorWorkflowInvoker>(sp);
    }

    public ProtoActorFeature ConfigureProtoActorBuilder(Func<IServiceProvider, ProtoActorSystem> factory)
    {
        ProtoActorBuilderFactory = factory;
        return this;
    }

    //configure the default one
    public Func<IServiceProvider, ProtoActorSystem> ProtoActorBuilderFactory { get; set; } =
        _ => new ProtoActorBuilder().UseLocalhostProvider("elsa-cluster", true) .Build();

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

            var workflowDefinitionProps = system.DI().PropsFor<WorkflowDefinitionGrainActor>();
            var workflowInstanceProps = system.DI().PropsFor<WorkflowInstanceGrainActor>();

            var clusterConfig =
                    ClusterConfig
                        .Setup(protoActorSystem.Name, protoActorSystem.ClusterProvider, protoActorSystem.IdentityLookup)
                        .WithHeartbeatExpiration(protoActorSystem.ClusterConfigurationSettings.HeartBeatExpiration)
                        .WithActorRequestTimeout(protoActorSystem.ClusterConfigurationSettings.ActorRequestTimeout)
                        .WithActorActivationTimeout(protoActorSystem.ClusterConfigurationSettings.ActorActivationTimeout)
                        .WithActorSpawnTimeout(protoActorSystem.ClusterConfigurationSettings.ActorSpawnTimeout)
                        .WithClusterKind(WorkflowDefinitionGrainActor.Kind, workflowDefinitionProps)
                        .WithClusterKind(WorkflowInstanceGrainActor.Kind, workflowInstanceProps);

            system
                .WithRemote(remoteConfig)
                .WithCluster(clusterConfig);

            return system;
        });

        // Cluster.
        services.AddSingleton(sp => sp.GetRequiredService<ActorSystem>().Cluster());

        // Actors.
        services
            .AddSingleton(sp => new WorkflowDefinitionGrainActor((context, _) =>
                ActivatorUtilities.CreateInstance<WorkflowDefinitionGrain>(sp, context)))
            .AddSingleton(sp => new WorkflowInstanceGrainActor((context, _) =>
                ActivatorUtilities.CreateInstance<WorkflowInstanceGrain>(sp, context)));

        // Client factory.
        services.AddSingleton<GrainClientFactory>();
    }

    public override void ConfigureHostedServices()
    {
        Services.AddHostedService<WorkflowServerHost>();
    }
}