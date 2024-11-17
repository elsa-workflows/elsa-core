using Google.Protobuf.Reflection;
using Proto;
using Proto.Cluster;

namespace Elsa.ProtoActor;

/// <summary>
/// Implement this to provide virtual actors to the actor system from other modules.
/// </summary>
public interface IVirtualActorsProvider
{
    /// Return a list of <see cref="ClusterKind"/> objects.
    IEnumerable<ClusterKind> GetClusterKinds(ActorSystem system);
    
    /// Return all file descriptors for protobuf serialization.
    IEnumerable<FileDescriptor> GetFileDescriptors();
}