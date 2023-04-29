namespace Proto.Cluster.AzureContainerApps.Stores.ResourceTags;

public static class ResourceTagLabels
{
    public static string LabelPrefix(string memberId) => $"proto.cluster-{memberId}|";
    public static string LabelHost(string memberId) => LabelPrefix(memberId) + "host";
    public static string LabelPort(string memberId) => LabelPrefix(memberId) + "port";
    public static string LabelKind(string memberId) => LabelPrefix(memberId) + "kind";
    public static string LabelCluster(string memberId) => LabelPrefix(memberId) + "cluster";
    public static string LabelMemberId(string memberId) => LabelPrefix(memberId) + LabelMemberIdWithoutPrefix;
    public const string LabelMemberIdWithoutPrefix = "memberId";
    public static string LabelReplicaName(string memberId) => LabelPrefix(memberId) + LabelReplicaNameWithoutPrefix;
    public const string LabelReplicaNameWithoutPrefix = "replicaName";
}