namespace Elsa.Identity.Endpoints.Permissions.List;

/// <summary>The permission catalog: every resource the installed modules have registered.</summary>
/// <param name="CoreVerbs">The recommended verb set modules should reuse. A convention, not a closed vocabulary.</param>
/// <param name="Resources">Every registered resource, ordered by name.</param>
public record Response(IReadOnlyCollection<string> CoreVerbs, IReadOnlyCollection<ResourceDescriptor> Resources);

/// <summary>One registered resource and the verbs it accepts.</summary>
/// <param name="NonCoreVerbs">
/// The subset of <paramref name="SupportedVerbs"/> outside the recommended core set, surfaced so a
/// reviewer can spot a needless synonym.
/// </param>
/// <param name="Verified">
/// <c>false</c> when the descriptor was inferred from an endpoint declaration rather than declared by a
/// module. The module keeps working and the gap stays visible here.
/// </param>
public record ResourceDescriptor(
    string Resource,
    IReadOnlyCollection<string> SupportedVerbs,
    IReadOnlyCollection<string> NonCoreVerbs,
    string DisplayName,
    string Description,
    string Category,
    bool Verified);
