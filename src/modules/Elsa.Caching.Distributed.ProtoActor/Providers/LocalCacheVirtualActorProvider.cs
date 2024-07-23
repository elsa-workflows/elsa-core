using Elsa.Caching.Distributed.ProtoActor.ProtoBuf;
using Elsa.ProtoActor;
using Elsa.ProtoActor.MemberStrategies;
using Google.Protobuf.Reflection;
using Proto;
using Proto.Cluster;
using Proto.DependencyInjection;

namespace Elsa.Caching.Distributed.ProtoActor.Providers;

public class LocalCacheVirtualActorProvider : VirtualActorsProviderBase
{
    public override IEnumerable<ClusterKind> GetClusterKinds(ActorSystem system)
    {
        var props = system.DI().PropsFor<LocalCacheActor>();
        yield return new ClusterKind(LocalCacheActor.Kind, props).WithMemberStrategy(cluster => new LocalNodeStrategy(cluster));
    }

    public override IEnumerable<FileDescriptor> GetFileDescriptors()
    {
        yield return LocalCacheMessagesReflection.Descriptor;
    }
}