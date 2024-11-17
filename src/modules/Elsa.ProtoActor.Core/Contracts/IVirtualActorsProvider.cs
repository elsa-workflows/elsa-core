using Google.Protobuf.Reflection;
using Proto;
using Proto.Cluster;

namespace Elsa.ProtoActor;

/// <summary>
/// Implement this to provide virtual actors to the actor system from other modules.
/// </summary>
public interface IVirtualActorsProvider
{
    /// <summary>
    /// Return a list of <see cref="ClusterKind"/> objects.
    /// </summary>
    IEnumerable<ClusterKind> GetClusterKinds(ActorSystem system);
    
    /// <summary>
    /// Return all file descriptors for protobuf serialization.
    /// </summary>
    IEnumerable<FileDescriptor> GetFileDescriptors();
}