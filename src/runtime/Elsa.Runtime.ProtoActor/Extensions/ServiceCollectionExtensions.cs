using System;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.ProtoActor.Actors;
using Elsa.Runtime.ProtoActor.HostedServices;
using Elsa.Runtime.ProtoActor.Services;
using Microsoft.Extensions.DependencyInjection;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Partition;
using Proto.Cluster.Testing;
using Proto.DependencyInjection;
using Proto.Remote;
using Proto.Remote.GrpcCore;

namespace Elsa.Runtime.ProtoActor.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProtoActorWorkflowHost(this IServiceCollection services)
    {
        var systemConfig = GetSystemConfig();

        services.AddSingleton(sp =>
        {
            var system = new ActorSystem(systemConfig).WithServiceProvider(sp);
            var remoteConfig = GetRemoteConfig();
            var clusterConfig = GetClusterConfig(system, "my-cluster");
            
            system
                .WithRemote(remoteConfig)
                .WithCluster(clusterConfig);

            return system;
        });

        services.AddSingleton(sp => sp.GetRequiredService<ActorSystem>().Cluster());

        return services
            .AddHostedService<WorkflowServerHost>()
            .AddSingleton<IWorkflowInvoker, ProtoActorWorkflowInvoker>()
            .AddTransient<WorkflowDefinitionActor>()
            .AddTransient<WorkflowInstanceActor>()
            .AddTransient<WorkflowOperatorActor>();
    }
        
    private static ActorSystemConfig GetSystemConfig() =>

        ActorSystemConfig
            .Setup()
            .WithDeadLetterThrottleCount(3)
            .WithDeadLetterThrottleInterval(TimeSpan.FromSeconds(1))
            .WithDeveloperSupervisionLogging(true)
            .WithDeadLetterRequestLogging(true);
        
    private static GrpcCoreRemoteConfig GetRemoteConfig() => GrpcCoreRemoteConfig
        .BindToLocalhost()
        .WithProtoMessages(Messages.MessagesReflection.Descriptor)
    ;
        
    private static ClusterConfig GetClusterConfig(ActorSystem system, string clusterName)
    {
        //var clusterProvider = new ConsulProvider(new ConsulProviderConfig{});
        var clusterProvider = new TestProvider(new TestProviderOptions(), new InMemAgent());
            
        var workflowDefinitionProps = system.DI().PropsFor<WorkflowDefinitionActor>();
        var workflowInstanceProps = system.DI().PropsFor<WorkflowInstanceActor>();

        var clusterConfig =
                ClusterConfig
                    // .Setup("MyCluster", clusterProvider, new IdentityStorageLookup(GetIdentityLookup(clusterName)))
                    .Setup(clusterName, clusterProvider, new PartitionIdentityLookup())
                    .WithClusterKind(GrainKinds.WorkflowDefinition, workflowDefinitionProps)
                    .WithClusterKind(GrainKinds.WorkflowInstance, workflowInstanceProps)
            ;
        return clusterConfig;
    }
        
    // private static IIdentityStorage GetIdentityLookup(string clusterName) =>
    //     new RedisIdentityStorage(clusterName, ConnectionMultiplexer
    //         .Connect("localhost:6379" /* use proper config */)
    //     );
}