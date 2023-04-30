namespace Proto.Cluster.AzureContainerApps.Stores.ResourceTags;

/// <summary>
/// Static helpers for creating Azure resource tag names.
/// </summary>
public static class ResourceTagNames
{
    /// <summary>
    /// The prefix for the tag name.
    /// </summary>
    public const string NamePrefix = "proto.cluster:member:";
    
    /// <summary>
    /// The prefix for the tag name.
    /// </summary>
    public const string KindPrefix = "kind:";
    
    /// <summary>
    /// Gets the prefixed name for the given member ID.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <returns>The prefixed name.</returns>
    public static string Prefix(string memberId) => $"{NamePrefix}{memberId}";
    
    /// <summary>
    /// Gets the prefixed name for the given member ID and name.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="name">The name.</param>
    /// <returns>The prefixed name.</returns>
    public static string Prefix(string memberId, string name) => $"{Prefix(memberId)}:{name}";

    /// <summary>
    /// Gets the name for the cluster tag for the given member ID.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="kind">The kind.</param>
    /// <returns>The prefixed kind.</returns>
    public static string PrefixKind(string memberId, string kind) => Prefix(memberId, $"{KindPrefix}{kind}");
}