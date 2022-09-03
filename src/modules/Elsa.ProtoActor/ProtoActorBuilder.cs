using Elsa.ProtoActor.Options;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Identity;
using Proto.Remote.GrpcNet;

namespace Elsa.ProtoActor;

public class ProtoActorBuilder
{
    ProtoActorSystem protoActorSystem = new ();
    public ProtoActorBuilder WithClusterProvider(IClusterProvider clusterProvider)
    {
        protoActorSystem.ClusterProvider = clusterProvider;
        return this;
    }

    public ProtoActorBuilder WithRemoteConfig(GrpcNetRemoteConfig remoteConfig)
    {
        protoActorSystem.RemoteConfig = remoteConfig;
        return this;
    }

    public ProtoActorSystem Build() => protoActorSystem;
    
    public ProtoActorBuilder WithOptions()
    {
        return this;
    }

    public ProtoActorBuilder WithIdentity(IIdentityLookup identityLookup)
    {
        protoActorSystem.IdentityLookup = identityLookup;
        return this;
    }
    
    public ProtoActorBuilder WithClusterName(string name)
    {
        protoActorSystem.Name = name;
        return this;
    }
    
    public ProtoActorBuilder WithClusterConfiguration(ClusterConfigurationSettings settings)
    {
        protoActorSystem.ClusterConfigurationSettings = settings;
        return this;
    }

    public ProtoActorBuilder WithActorSystemConfig(ActorSystemConfig actorSystemConfig)
    {
        protoActorSystem.ActorSystemConfig = actorSystemConfig;
        return this;
    }
}