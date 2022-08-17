using Elsa.ProtoActor.Common.Options;
using Proto.Cluster.Identity;
using Proto.Cluster.Partition;

namespace Elsa.ProtoActor.Kubernetes;

public class KubernetesProviderOptions : ProviderOptions
{
    public string HostAddress { get; set; }

    public IIdentityLookup IdentityLookup { get; set; } = new PartitionIdentityLookup();
}