using Google.Protobuf.Reflection;
using Proto;
using Proto.Cluster;

namespace Elsa.ProtoActor;

/// Implement this to provide a virtual actor to the actor system.
public interface IVirtualActorsProvider
{
    /// The actor type name and props to activate the virtual actor.
    IEnumerable<ClusterKind> GetClusterKinds(ActorSystem system);
    
    /// <summary>
    /// Return all file descriptors for protobuf serialization.
    /// </summary>
    IEnumerable<FileDescriptor> GetFileDescriptors();
}