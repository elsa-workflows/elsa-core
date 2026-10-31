using System.Text.Json;

namespace Elsa.Studio.Security.Models;

/// <summary>One user returned by the Identity user endpoints. Never carries credential material.</summary>
public class UserSummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ICollection<string> Roles { get; set; } = [];
    public string? TenantId { get; set; }
}

public sealed class ListUsersResponse
{
    public ICollection<UserSummary> Users { get; set; } = [];
}

public sealed class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Password { get; set; }
    public ICollection<string> Roles { get; set; } = [];
}

/// <summary>
/// Response from <c>POST /identity/users</c>. <see cref="GeneratedPassword"/> is present only when Core generated the
/// password because none was supplied; it is shown once and cannot be retrieved again.
/// </summary>
public sealed class CreateUserResponse : UserSummary
{
    public string? GeneratedPassword { get; set; }
}

public sealed class UpdateUserRequest
{
    public string? Password { get; set; }
    public ICollection<string>? Roles { get; set; }
}

/// <summary>One role returned by the Identity role list endpoint.</summary>
public sealed record RoleSummary
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public ICollection<string> Permissions { get; init; } = [];
    public string? TenantId { get; init; }
}

/// <summary>Response from <c>GET /identity/roles</c>.</summary>
public sealed record ListRolesResponse
{
    public ICollection<RoleSummary> Roles { get; init; } = [];
}

/// <summary>Request sent to <c>POST /identity/roles</c>. Core generates the ID.</summary>
public sealed record CreateRoleRequest
{
    public string Name { get; init; } = string.Empty;
    public ICollection<string>? Permissions { get; init; }
}

/// <summary>Response from <c>POST /identity/roles</c>.</summary>
public sealed record CreateRoleResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public ICollection<string> Permissions { get; init; } = [];
}

/// <summary>Request sent to <c>PUT /identity/roles/{id}</c>.</summary>
public sealed record UpdateRoleRequest
{
    public string? Name { get; init; }

    /// <summary>A null value leaves permissions unchanged; an empty collection explicitly clears them.</summary>
    public ICollection<string>? Permissions { get; init; }
}

/// <summary>Response from <c>PUT /identity/roles/{id}</c>.</summary>
public sealed record UpdateRoleResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public ICollection<string> Permissions { get; init; } = [];
    public string? TenantId { get; init; }
}

/// <summary>Response from <c>GET /identity/permissions</c>.</summary>
public sealed record PermissionCatalogResponse
{
    public IReadOnlyCollection<string> CoreVerbs { get; init; } = [];
    public IReadOnlyCollection<PermissionResourceDescriptor> Resources { get; init; } = [];
}

/// <summary>One permission resource registered by the Core descriptor registry.</summary>
public sealed record PermissionResourceDescriptor
{
    public string Resource { get; init; } = string.Empty;
    public IReadOnlyCollection<string> SupportedVerbs { get; init; } = [];
    public IReadOnlyCollection<string> NonCoreVerbs { get; init; } = [];
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public bool Verified { get; init; }
}

/// <summary>Response from <c>GET /identity/me/permissions</c>.</summary>
public sealed record CurrentCallerPermissionsResponse
{
    public IReadOnlyCollection<CurrentCallerResourceGrant> Grants { get; init; } = [];
}

/// <summary>Effective concrete verbs held by the current caller for one resource.</summary>
public sealed record CurrentCallerResourceGrant
{
    public string Resource { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Verbs { get; init; } = [];
}

/// <summary>Response from <c>GET /identity/permissions/reach</c>.</summary>
public sealed record PermissionReachResponse
{
    public string Resource { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Covers { get; init; } = [];
    public int Count { get; init; }
}

/// <summary>Versioned impact snapshot returned before deleting a role.</summary>
public sealed record RoleDeletionImpactResponse
{
    public string RoleId { get; init; } = string.Empty;
    public string DependencyVersion { get; init; } = string.Empty;
    public string ExecutionMode { get; init; } = string.Empty;
    public bool CanDelete { get; init; }
    public bool CanRemediate { get; init; }
    public IReadOnlyCollection<RoleDeletionDependencyResponse> ConfigurationReferences { get; init; } = [];
    public IReadOnlyCollection<RoleDeletionDependencyResponse> EditableReferences { get; init; } = [];
    public IReadOnlyCollection<string> Warnings { get; init; } = [];
}

/// <summary>One dependency reported by the role deletion impact endpoint.</summary>
public sealed record RoleDeletionDependencyResponse
{
    public string Source { get; init; } = string.Empty;
    public string OwnerId { get; init; } = string.Empty;
    public string OwnerKey { get; init; } = string.Empty;
    public string PolicyBranch { get; init; } = string.Empty;
    public string Ownership { get; init; } = string.Empty;
    public string? ConfigurationPath { get; init; }
    public long? Revision { get; init; }
    public bool RemovesLastDefaultRole { get; init; }
}

/// <summary>Explicit confirmations and the inspected version sent for remediation.</summary>
public sealed record RoleRemediationRequest
{
    public string? ExpectedDependencyVersion { get; init; }
    public bool ConfirmRemoveFromEditableJitPolicies { get; init; }
    public bool ConfirmEmptyDefaultRoles { get; init; }
    public bool ConfirmBestEffort { get; init; }
    public IReadOnlyCollection<RoleDeletionReferenceSelection>? SelectedReferences { get; init; }
    public string? ReplacementRoleId { get; init; }
}

/// <summary>Identifies one editable role reference selected for remediation.</summary>
public sealed record RoleDeletionReferenceSelection
{
    public string Source { get; init; } = string.Empty;
    public string OwnerId { get; init; } = string.Empty;
}

/// <summary>Core's structured error shape used by role deletion and other Identity endpoints.</summary>
public sealed record CoreApiErrorResponse
{
    public string Error { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public JsonElement? Details { get; init; }
}

/// <summary>FastEndpoints validation error shape returned by Identity mutations.</summary>
public sealed record ValidationApiErrorResponse
{
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public Dictionary<string, List<string>> Errors { get; init; } = [];
}
