using Google.Protobuf.Reflection;
using Proto;
using Proto.Cluster;

namespace Elsa.ProtoActor;

public abstract class VirtualActorsProviderBase : IVirtualActorsProvider
{
    public abstract IEnumerable<ClusterKind> GetClusterKinds(ActorSystem system);
    public virtual IEnumerable<FileDescriptor> GetFileDescriptors() => [];
}