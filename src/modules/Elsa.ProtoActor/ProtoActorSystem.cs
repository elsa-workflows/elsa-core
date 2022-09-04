using Elsa.ProtoActor.Options;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Identity;
using Proto.Remote.GrpcNet;

namespace Elsa.ProtoActor;

public class ProtoActorSystem
{
    public ProtoActorSystem()
    {
    }
    
    public ProtoActorSystem(
        IClusterProvider clusterProvider, 
        GrpcNetRemoteConfig remoteConfig, 
        ActorSystemConfig actorSystemConfig, 
        IIdentityLookup identityLookup, 
        string name, 
        ClusterConfigurationSettings clusterConfigurationSettings)
    {
        ClusterProvider = clusterProvider;
        RemoteConfig = remoteConfig;
        ActorSystemConfig = actorSystemConfig;
        IdentityLookup = identityLookup;
        Name = name;
        ClusterConfigurationSettings = clusterConfigurationSettings;
    }

    public IClusterProvider ClusterProvider { get; set; }
    public GrpcNetRemoteConfig RemoteConfig { get; set; }
    public ActorSystemConfig ActorSystemConfig { get; set; } = ActorSystemConfig.Setup();
    public IIdentityLookup IdentityLookup { get; set; }
    public ClusterConfigurationSettings ClusterConfigurationSettings { get; set; } = new();
    public string Name { get; set; }
}