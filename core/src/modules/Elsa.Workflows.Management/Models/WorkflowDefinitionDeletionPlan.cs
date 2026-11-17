using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Models;

/// <summary>
/// The version identities authorized for deletion and the surviving versions whose latest flags must change.
/// </summary>
public sealed class WorkflowDefinitionDeletionPlan
{
    private readonly Dictionary<string, WorkflowDefinition> _expected;

    /// <summary>
    /// Builds a plan from the selected versions and all visible versions of their definitions.
    /// </summary>
    public WorkflowDefinitionDeletionPlan(ICollection<WorkflowDefinition> selected, IEnumerable<WorkflowDefinition> observed)
    {
        _expected = observed.ToDictionary(version => version.Id, version => version.ShallowClone(), StringComparer.Ordinal);
        var selectedIds = selected.Select(version => version.Id).ToHashSet(StringComparer.Ordinal);
        Deleted = _expected.Values.Where(version => selectedIds.Contains(version.Id)).ToArray();
        DefinitionIds = selected.Select(version => version.DefinitionId).Distinct(StringComparer.Ordinal).ToArray();
        IsComplete = selectedIds.Count == Deleted.Count;
        var affected = Deleted.Select(DefinitionKey).ToHashSet();
        Promoted = _expected.Values.Where(version => !selectedIds.Contains(version.Id) && affected.Contains(DefinitionKey(version)))
            .GroupBy(DefinitionKey)
            .Select(group => group.MaxBy(version => version.Version)!)
            .Where(version => !version.IsLatest).ToArray();
    }

    /// <summary>The logical definition IDs to check.</summary>
    public ICollection<string> DefinitionIds { get; }

    /// <summary>The concrete versions authorized for deletion.</summary>
    public IReadOnlyCollection<WorkflowDefinition> Deleted { get; }

    /// <summary>The versions authorized to become latest.</summary>
    public IReadOnlyCollection<WorkflowDefinition> Promoted { get; }

    /// <summary>Whether the observed version set included every requested version.</summary>
    public bool IsComplete { get; }

    /// <summary>Checks whether the stored versions still match the authorized identities and lifecycle state.</summary>
    public bool Matches(IReadOnlyCollection<WorkflowDefinition> current) =>
        IsComplete && current.Count == _expected.Count && current.All(version =>
            _expected.TryGetValue(version.Id, out var expected) && version.DefinitionId == expected.DefinitionId &&
            version.TenantId == expected.TenantId && version.Version == expected.Version && version.CreatedAt == expected.CreatedAt &&
            version.IsLatest == expected.IsLatest && version.IsPublished == expected.IsPublished &&
            version.IsReadonly == expected.IsReadonly && version.IsSystem == expected.IsSystem &&
            version.Name == expected.Name && version.StringData == expected.StringData && version.OriginalSource == expected.OriginalSource);

    private static (string DefinitionId, string TenantId) DefinitionKey(WorkflowDefinition version) =>
        (version.DefinitionId, version.TenantId.NormalizeTenantId());
}
