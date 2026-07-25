using System.Security.Claims;

namespace Elsa.Identity.Models;

/// <summary>Identifies whether a role dependency can be changed through an administration API.</summary>
public enum RoleDeletionDependencyOwnership
{
    Configuration,
    Database
}

/// <summary>Describes how a role-remediation command will be executed.</summary>
public enum RoleDeletionExecutionMode
{
    Atomic,
    BestEffort
}

/// <summary>A safe reference to a role from an installed module.</summary>
public sealed record RoleDeletionDependency(
    string Source,
    string OwnerId,
    string OwnerKey,
    string PolicyBranch,
    RoleDeletionDependencyOwnership Ownership,
    string? ConfigurationPath,
    long? ExpectedRevision,
    bool RemovesLastDefaultRole);

/// <summary>A contributor-owned dependency snapshot.</summary>
public sealed record RoleDeletionDependencySnapshot(
    string Source,
    string Version,
    bool SupportsAtomicRemoval,
    IReadOnlyCollection<RoleDeletionDependency> Dependencies);

/// <summary>The aggregated impact of deleting a role.</summary>
public sealed record RoleDeletionImpact(
    string RoleId,
    string DependencyVersion,
    RoleDeletionExecutionMode ExecutionMode,
    bool CanDelete,
    bool CanRemediate,
    IReadOnlyCollection<RoleDeletionDependency> Dependencies);

/// <summary>Inputs used to prevalidate or remove one contributor's editable references.</summary>
public sealed record RoleReferenceRemovalRequest(
    string RoleId,
    ClaimsPrincipal Actor,
    string ExpectedContributorVersion,
    IReadOnlyCollection<RoleDeletionDependency> Dependencies);

/// <summary>Inputs for the coordinated remediation command.</summary>
public sealed record RoleDeletionRemediationCommand(
    string RoleId,
    ClaimsPrincipal Actor,
    string ExpectedDependencyVersion,
    bool ConfirmRemoveFromEditablePolicies,
    bool ConfirmEmptyDefaultRoles,
    bool ConfirmBestEffort);

public abstract record RoleReferenceRemovalValidationResult
{
    private RoleReferenceRemovalValidationResult()
    {
    }

    public sealed record Valid : RoleReferenceRemovalValidationResult;
    public sealed record Forbidden(string Code) : RoleReferenceRemovalValidationResult;
    public sealed record Conflict(string Code) : RoleReferenceRemovalValidationResult;
}

public abstract record RoleReferenceRemovalResult
{
    private RoleReferenceRemovalResult()
    {
    }

    public sealed record Success(IReadOnlyCollection<string> ChangedOwnerIds) : RoleReferenceRemovalResult;
    public sealed record Conflict(string Code, IReadOnlyCollection<string> ChangedOwnerIds) : RoleReferenceRemovalResult;
    public sealed record Failed(string Code, IReadOnlyCollection<string> ChangedOwnerIds) : RoleReferenceRemovalResult;
}

public abstract record RoleDeletionInspectionResult
{
    private RoleDeletionInspectionResult()
    {
    }

    public sealed record Success(RoleDeletionImpact Impact) : RoleDeletionInspectionResult;
    public sealed record NotFound : RoleDeletionInspectionResult;
    public sealed record Forbidden : RoleDeletionInspectionResult;
}

public abstract record RoleDeletionOperationResult
{
    private RoleDeletionOperationResult()
    {
    }

    public sealed record Deleted(IReadOnlyCollection<string> ChangedOwnerIds) : RoleDeletionOperationResult;
    public sealed record NotFound : RoleDeletionOperationResult;
    public sealed record Forbidden : RoleDeletionOperationResult;
    public sealed record Blocked(RoleDeletionImpact Impact) : RoleDeletionOperationResult;
    public sealed record PreconditionFailed(RoleDeletionImpact Impact) : RoleDeletionOperationResult;
    public sealed record ConfirmationRequired(RoleDeletionImpact Impact, IReadOnlyCollection<string> Warnings) : RoleDeletionOperationResult;
    public sealed record Incomplete(RoleDeletionImpact Impact, IReadOnlyCollection<string> ChangedOwnerIds, string Code) : RoleDeletionOperationResult;
}
