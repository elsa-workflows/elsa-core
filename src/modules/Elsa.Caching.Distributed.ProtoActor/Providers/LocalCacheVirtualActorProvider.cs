using Elsa.Caching.Distributed.ProtoActor.ProtoBuf;
using Elsa.ProtoActor;
using Google.Protobuf.Reflection;
using Proto;
using Proto.Cluster;
using Proto.DependencyInjection;

namespace Elsa.Caching.Distributed.ProtoActor.Providers;

public class LocalCacheVirtualActorProvider  : VirtualActorsProviderBase
{
    public override IEnumerable<ClusterKind> GetClusterKinds(ActorSystem system)
    {
        var props = system.DI().PropsFor<LocalCacheActor>();
        yield return new ClusterKind(LocalCacheActor.Kind, props);
    }
    
    public override IEnumerable<FileDescriptor> GetFileDescriptors()
    {
        yield return SharedReflection.Descriptor;
        yield return LocalCacheMessagesReflection.Descriptor;
    }
}