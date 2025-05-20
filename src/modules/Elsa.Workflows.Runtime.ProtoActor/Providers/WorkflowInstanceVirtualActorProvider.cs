using Elsa.ProtoActor;
using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;
using Google.Protobuf.Reflection;
using Proto;
using Proto.Cluster;
using Proto.DependencyInjection;

namespace Elsa.Workflows.Runtime.ProtoActor.Providers;

public class WorkflowInstanceVirtualActorProvider : VirtualActorsProviderBase
{
    public override IEnumerable<ClusterKind> GetClusterKinds(ActorSystem system)
    {
        var props = system.DI().PropsFor<WorkflowInstanceActor>();
        yield return new ClusterKind(WorkflowInstanceActor.Kind, props);
    }

    public override IEnumerable<FileDescriptor> GetFileDescriptors()
    {
        yield return SharedReflection.Descriptor;
        yield return WorkflowInstanceMessagesReflection.Descriptor;
    }
}