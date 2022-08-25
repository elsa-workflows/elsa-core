using System;
using Elsa.ProtoActor.Common;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Kubernetes;
using Proto.Cluster.Partition;
using Proto.Remote;
using Proto.Remote.GrpcNet;

namespace Elsa.ProtoActor.Kubernetes;

public static class ProtoActorBuilderExtensions
{
    public static ProtoActorBuilder UseKubernetesProvider(this ProtoActorBuilder builder, KubernetesProviderOptions options)
    {
        var (remoteConfig, clusterProvider) = ConfigureForKubernetes(options.HostAddress);

        var actorSystemConfig = ActorSystemConfig.Setup();
        if (options.WithDeveloperLogging)
        {
            actorSystemConfig.WithDeveloperLogging();
        }

        if (options.WithMetrics)
        {
            actorSystemConfig.WithMetrics();
        }
        
        builder.WithClusterProvider(clusterProvider)
            .WithRemoteConfig(remoteConfig)
                .WithClusterName(options.Name)
                .WithIdentity(options.IdentityLookup)
                .WithActorSystemConfig(actorSystemConfig);
        return builder;
    }
    
    private static (GrpcNetRemoteConfig, IClusterProvider) ConfigureForKubernetes(string host)
    {
        var clusterProvider = new KubernetesProvider();

        var remoteConfig = GrpcNetRemoteConfig
            .BindToAllInterfaces(advertisedHost: host)
            .WithLogLevelForDeserializationErrors(LogLevel.Critical)
            .WithRemoteDiagnostics(true);

        return (remoteConfig, clusterProvider);
    }
}