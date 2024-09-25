using Google.Protobuf.Reflection;
using Proto;
using Proto.Cluster;

namespace Elsa.ProtoActor;

/// Implement this to provide virtual actors to the actor system from other modules.
public interface IVirtualActorsProvider
{
    /// Return a list of <see cref="ClusterKind"/> objects.
    IEnumerable<ClusterKind> GetClusterKinds(ActorSystem system);
    
    /// Return all file descriptors for protobuf serialization.
    IEnumerable<FileDescriptor> GetFileDescriptors();
}