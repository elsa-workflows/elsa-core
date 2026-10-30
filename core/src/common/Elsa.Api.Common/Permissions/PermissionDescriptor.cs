namespace Elsa.Permissions;

/// <summary>
/// Module-contributed metadata for one resource: what it is called, what it means, and which verbs it
/// supports. The catalog built from these descriptors is what lets a role editor render without
/// hard-coding permission strings, and what the coverage gate validates endpoint declarations against.
/// </summary>
/// <param name="Resource">The hierarchical resource path, for example <c>workflows/definitions</c>.</param>
/// <param name="SupportedVerbs">
/// The verbs this resource accepts. A resource declares either <c>create</c> + <c>update</c>, or
/// <c>write</c> — never both. The wildcard is deliberately absent: it is not a verb a user selects.
/// </param>
/// <param name="DisplayName">A human-readable name for a role editor.</param>
/// <param name="Description">What granting this resource allows.</param>
/// <param name="Category">A grouping for presentation, such as <c>Workflows</c>.</param>
/// <param name="Verified">
/// <c>false</c> for an implicit descriptor auto-registered because a module declared a permission that
/// resolved to no declared descriptor. The module keeps working and the gap stays visible in the catalog.
/// </param>
public sealed record PermissionDescriptor(
    string Resource,
    IReadOnlyCollection<string> SupportedVerbs,
    string DisplayName,
    string Description,
    string Category,
    bool Verified = true)
{
    /// <summary>The verbs this resource supports that fall outside the recommended core set.</summary>
    public IReadOnlyCollection<string> NonCoreVerbs { get; } =
        SupportedVerbs.Where(x => !Authorization.CoreVerbs.IsCore(x)).ToArray();

    /// <summary>Whether this resource supports <paramref name="verb"/>.</summary>
    public bool Supports(string verb) => SupportedVerbs.Contains(verb, StringComparer.Ordinal);
}
