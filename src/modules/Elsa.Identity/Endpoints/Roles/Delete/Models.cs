using Elsa.Identity.Models;

namespace Elsa.Identity.Endpoints.Roles.Delete;

internal sealed class RemediateRoleDeletionRequest
{
    public string? ExpectedDependencyVersion { get; set; }
    public bool ConfirmRemoveFromEditableJitPolicies { get; set; }
    public bool ConfirmEmptyDefaultRoles { get; set; }
    public bool ConfirmBestEffort { get; set; }
}

internal sealed record RoleDeletionErrorResponse(string Error, string Message, object? Details = null);

internal sealed record RoleDeletionImpactResponse(
    string RoleId,
    string DependencyVersion,
    string ExecutionMode,
    bool CanDelete,
    bool CanRemediate,
    IReadOnlyCollection<RoleDeletionDependencyResponse> ConfigurationReferences,
    IReadOnlyCollection<RoleDeletionDependencyResponse> EditableReferences,
    IReadOnlyCollection<string> Warnings)
{
    public static RoleDeletionImpactResponse From(RoleDeletionImpact impact)
    {
        var references = impact.Dependencies.Select(RoleDeletionDependencyResponse.From).ToArray();
        return new RoleDeletionImpactResponse(
            impact.RoleId,
            impact.DependencyVersion,
            impact.ExecutionMode == RoleDeletionExecutionMode.Atomic ? "atomic" : "bestEffort",
            impact.CanDelete,
            impact.CanRemediate,
            references.Where(x => x.Ownership == "configuration").ToArray(),
            references.Where(x => x.Ownership == "database").ToArray(),
            impact.Dependencies.Any(x => x.RemovesLastDefaultRole) ? ["removes_last_default_role"] : []);
    }
}

internal sealed record RoleDeletionDependencyResponse(
    string Source,
    string OwnerId,
    string OwnerKey,
    string PolicyBranch,
    string Ownership,
    string? ConfigurationPath,
    long? Revision,
    bool RemovesLastDefaultRole)
{
    public static RoleDeletionDependencyResponse From(RoleDeletionDependency dependency) => new(
        dependency.Source,
        dependency.OwnerId,
        dependency.OwnerKey,
        dependency.PolicyBranch,
        dependency.Ownership == RoleDeletionDependencyOwnership.Configuration ? "configuration" : "database",
        dependency.ConfigurationPath,
        dependency.ExpectedRevision,
        dependency.RemovesLastDefaultRole);
}
