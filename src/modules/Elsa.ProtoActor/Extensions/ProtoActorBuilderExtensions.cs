using Proto;
using Proto.Cluster.Partition;
using Proto.Cluster.Testing;
using Proto.Remote.GrpcNet;

namespace Elsa.ProtoActor.Extensions;

public static class ProtoActorBuilderExtensions
{
    public static ProtoActorBuilder UseLocalhostProvider(this ProtoActorBuilder builder, string clusterName, bool withDeveloperLogging)
    {
        var actorSystemConfig = ActorSystemConfig.Setup();
        
        builder.WithClusterProvider(new TestProvider(new TestProviderOptions(), new InMemAgent()))
            .WithRemoteConfig(GrpcNetRemoteConfig
                .BindToLocalhost())
            .WithClusterName(clusterName)
            .WithIdentity(new PartitionIdentityLookup())
            .WithActorSystemConfig(withDeveloperLogging ? actorSystemConfig.WithDeveloperLogging() : actorSystemConfig);
        return builder;
    }
}