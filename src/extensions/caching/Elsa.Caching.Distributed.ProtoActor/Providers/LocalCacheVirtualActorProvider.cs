using Elsa.Caching.Distributed.ProtoActor.ProtoBuf;
using Elsa.Actors.ProtoActor;
using Google.Protobuf.Reflection;
using Proto;
using Proto.Cluster;

namespace Elsa.Caching.Distributed.ProtoActor.Providers;

public class LocalCacheVirtualActorProvider : VirtualActorsProviderBase
{
    public override IEnumerable<ClusterKind> GetClusterKinds(ActorSystem system) => [];
    public override IEnumerable<FileDescriptor> GetFileDescriptors()
    {
        yield return LocalCacheMessagesReflection.Descriptor;
    }
}