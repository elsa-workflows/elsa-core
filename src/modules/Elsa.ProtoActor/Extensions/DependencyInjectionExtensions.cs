using System;
using Elsa.ProtoActor.Grains;
using Elsa.ProtoActor.HostedServices;
using Elsa.ProtoActor.Implementations;
using Elsa.Runtime.Protos;
using Elsa.Workflows.Runtime.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Partition;
using Proto.Cluster.Testing;
using Proto.DependencyInjection;
using Proto.Remote;
using Proto.Remote.GrpcNet;

namespace Elsa.ProtoActor.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddProtoActorRuntime(this IServiceCollection services)
    {
        var systemConfig = GetSystemConfig();

        // Logging.
        Log.SetLoggerFactory(LoggerFactory.Create(l => l.AddConsole().SetMinimumLevel(LogLevel.Warning)));

        // Actor System.
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

        // Cluster.
        services.AddSingleton(sp => sp.GetRequiredService<ActorSystem>().Cluster());

        // Actors.
        services
            .AddSingleton(sp => new WorkflowDefinitionGrainActor((context, _) => ActivatorUtilities.CreateInstance<WorkflowDefinitionGrain>(sp, context)))
            .AddSingleton(sp => new WorkflowInstanceGrainActor((context, _) => ActivatorUtilities.CreateInstance<WorkflowInstanceGrain>(sp, context)));

        // Client factory.
        services.AddSingleton<GrainClientFactory>();

        // Configure runtime with ProtoActor workflow invoker.
        services.ConfigureWorkflowRuntime(options => options.WorkflowInvokerFactory = sp => ActivatorUtilities.CreateInstance<ProtoActorWorkflowInvoker>(sp));

        return services
            .AddHostedService<WorkflowServerHost>();
    }

    private static ActorSystemConfig GetSystemConfig() =>
        ActorSystemConfig
            .Setup()
            .WithDeveloperSupervisionLogging(true)
            .WithDeveloperReceiveLogging(TimeSpan.FromHours(1))
            .WithDeadLetterThrottleCount(3)
            .WithDeadLetterThrottleInterval(TimeSpan.FromSeconds(10000))
            .WithDeveloperSupervisionLogging(true)
            .WithDeadLetterRequestLogging(true);

    private static GrpcNetRemoteConfig GetRemoteConfig() => Proto.Remote.GrpcNet.GrpcNetRemoteConfig
        .BindToLocalhost()
        .WithProtoMessages(MessagesReflection.Descriptor);

    private static ClusterConfig GetClusterConfig(ActorSystem system, string clusterName)
    {
        //var clusterProvider = new ConsulProvider(new ConsulProviderConfig{});
        var clusterProvider = new TestProvider(new TestProviderOptions(), new InMemAgent());

        var workflowDefinitionProps = system.DI().PropsFor<WorkflowDefinitionGrainActor>();
        var workflowInstanceProps = system.DI().PropsFor<WorkflowInstanceGrainActor>();

        var clusterConfig =
                ClusterConfig
                    // .Setup("MyCluster", clusterProvider, new IdentityStorageLookup(GetIdentityLookup(clusterName)))
                    .Setup(clusterName, clusterProvider, new PartitionIdentityLookup())
                    .WithHeartbeatExpiration(TimeSpan.FromDays(1))
                    //.WithTimeout(TimeSpan.FromHours(1))
                    .WithActorRequestTimeout(TimeSpan.FromHours(1))
                    .WithActorActivationTimeout(TimeSpan.FromHours(1))
                    .WithActorSpawnTimeout(TimeSpan.FromHours(1))
                    .WithClusterKind(WorkflowDefinitionGrainActor.Kind, workflowDefinitionProps)
                    .WithClusterKind(WorkflowInstanceGrainActor.Kind, workflowInstanceProps)
            ;
        return clusterConfig;
    }

    // private static IIdentityStorage GetIdentityLookup(string clusterName) =>
    //     new RedisIdentityStorage(clusterName, ConnectionMultiplexer
    //         .Connect("localhost:6379" /* use proper config */)
    //     );
}