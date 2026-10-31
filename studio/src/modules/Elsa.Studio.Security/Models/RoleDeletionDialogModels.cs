namespace Elsa.Studio.Security.Models;

/// <summary>Classifies the impact inspection that precedes a role deletion.</summary>
public enum RoleDeletionInspectionOutcome
{
    Safe,
    Blocked,
    RemediationRequired,
    ConfirmationRequired,
    Forbidden,
    NotFound,
    Error
}

/// <summary>Classifies an attempted role deletion or editable-policy remediation.</summary>
public enum RoleDeletionOperationOutcome
{
    Deleted,
    Blocked,
    DependencyConflict,
    ConfirmationRequired,
    Incomplete,
    Forbidden,
    NotFound,
    Error
}

/// <summary>Presentation states used by the shared delete-role dialog.</summary>
public enum DeleteRoleDialogViewState
{
    Loading,
    SafeConfirmation,
    ConfigurationBlocked,
    Remediation,
    ConfirmationRequired,
    DependencyConflict,
    Incomplete,
    Forbidden,
    NotFound,
    Error
}

/// <summary>Display-ready result of the versioned deletion-impact inspection.</summary>
public sealed record RoleDeletionInspectionResult
{
    public RoleDeletionInspectionOutcome Outcome { get; init; }
    public RoleDeletionImpactResponse? Impact { get; init; }
    public string? Code { get; init; }
    public string? Message { get; init; }
    public IReadOnlyCollection<string> Warnings { get; init; } = [];
}

/// <summary>Display-ready result of a delete or remediation attempt.</summary>
public sealed record RoleDeletionOperationResult
{
    public RoleDeletionOperationOutcome Outcome { get; init; }
    public RoleDeletionImpactResponse? Impact { get; init; }
    public string? Code { get; init; }
    public string? Message { get; init; }
    public IReadOnlyCollection<string> Warnings { get; init; } = [];
    public IReadOnlyCollection<string> ChangedOwnerIds { get; init; } = [];
}

/// <summary>Explicit confirmations and the dependency snapshot used for remediation.</summary>
public sealed record RoleDeletionConfirmation
{
    public string? ExpectedDependencyVersion { get; init; }
    public bool ConfirmRemoveFromEditableJitPolicies { get; init; }
    public bool ConfirmEmptyDefaultRoles { get; init; }
    public bool ConfirmBestEffort { get; init; }
    public IReadOnlyCollection<RoleDeletionReferenceSelection> SelectedReferences { get; init; } = [];
    public string? ReplacementDefaultRoleId { get; init; }
}

/// <summary>Stable result returned by <see cref="Components.DeleteRoleDialog"/>.</summary>
public enum DeleteRoleDialogResultKind
{
    Cancelled,
    Deleted,
    Retained
}

/// <summary>Typed dialog result that callers can use to refresh a role list or editor after deletion.</summary>
public sealed record DeleteRoleDialogResult
{
    public string RoleId { get; init; } = string.Empty;
    public DeleteRoleDialogResultKind Outcome { get; init; }
    public IReadOnlyCollection<string> ChangedOwnerIds { get; init; } = [];
    public string? Code { get; init; }

    public bool WasDeleted => Outcome == DeleteRoleDialogResultKind.Deleted;
    public bool WasRetained => Outcome == DeleteRoleDialogResultKind.Retained;
    public bool ShouldRefresh => WasDeleted || ChangedOwnerIds.Count != 0;

    public static DeleteRoleDialogResult Cancelled(string roleId) => new()
    {
        RoleId = roleId,
        Outcome = DeleteRoleDialogResultKind.Cancelled
    };

    public static DeleteRoleDialogResult Deleted(string roleId, IReadOnlyCollection<string>? changedOwnerIds = null) => new()
    {
        RoleId = roleId,
        Outcome = DeleteRoleDialogResultKind.Deleted,
        ChangedOwnerIds = changedOwnerIds ?? []
    };

    public static DeleteRoleDialogResult Retained(
        string roleId,
        string? code = null,
        IReadOnlyCollection<string>? changedOwnerIds = null) => new()
    {
        RoleId = roleId,
        Outcome = DeleteRoleDialogResultKind.Retained,
        Code = code,
        ChangedOwnerIds = changedOwnerIds ?? []
    };
}
