using Elsa.ProtoActor.Options;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Identity;
using Proto.Remote.GrpcNet;

namespace Elsa.ProtoActor;

public class ProtoActorBuilder
{
    readonly ProtoActorSystem _protoActorSystem = new ();
    
    public ProtoActorBuilder WithClusterProvider(IClusterProvider clusterProvider)
    {
        _protoActorSystem.ClusterProvider = clusterProvider;
        return this;
    }

    public ProtoActorBuilder WithRemoteConfig(GrpcNetRemoteConfig remoteConfig)
    {
        _protoActorSystem.RemoteConfig = remoteConfig;
        return this;
    }

    public ProtoActorSystem Build() => _protoActorSystem;
    
    public ProtoActorBuilder WithOptions()
    {
        return this;
    }

    public ProtoActorBuilder WithIdentity(IIdentityLookup identityLookup)
    {
        _protoActorSystem.IdentityLookup = identityLookup;
        return this;
    }
    
    public ProtoActorBuilder WithClusterName(string name)
    {
        _protoActorSystem.Name = name;
        return this;
    }
    
    public ProtoActorBuilder WithClusterConfiguration(ClusterConfigurationSettings settings)
    {
        _protoActorSystem.ClusterConfigurationSettings = settings;
        return this;
    }

    public ProtoActorBuilder WithActorSystemConfig(ActorSystemConfig actorSystemConfig)
    {
        _protoActorSystem.ActorSystemConfig = actorSystemConfig;
        return this;
    }
}