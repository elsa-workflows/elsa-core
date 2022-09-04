using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.ProtoActor.Features;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Identity;
using Proto.Cluster.Kubernetes;
using Proto.Cluster.Partition;
using Proto.Remote;
using Proto.Remote.GrpcNet;

namespace Elsa.ProtoActor.Kubernetes;

[DependsOn(typeof(ProtoActorFeature))]
public class KubernetesProtoActorFeature : FeatureBase
{
    public KubernetesProtoActorFeature(IModule module) : base(module)
    {
    }
    
    public string HostAddress { get; set; } = default!;
    public IIdentityLookup IdentityLookup { get; set; } = new PartitionIdentityLookup();

    public override void Configure()
    {
        Module.Configure<ProtoActorFeature>().ConfigureActorBuilder = (sp, builder) =>
        {
            var (remoteConfig, clusterProvider) = ConfigureForKubernetes(HostAddress);
            var actorSystemConfig = ActorSystemConfig.Setup();
            
            builder.WithClusterProvider(clusterProvider)
                .WithRemoteConfig(remoteConfig)
                .WithIdentity(IdentityLookup)
                .WithActorSystemConfig(actorSystemConfig);
        };
    }

    private static (GrpcNetRemoteConfig, IClusterProvider) ConfigureForKubernetes(string host)
    {
        var clusterProvider = new KubernetesProvider();

        var remoteConfig = GrpcNetRemoteConfig
            .BindToAllInterfaces(host)
            .WithLogLevelForDeserializationErrors(LogLevel.Critical)
            .WithRemoteDiagnostics(true);

        return (remoteConfig, clusterProvider);
    }
}