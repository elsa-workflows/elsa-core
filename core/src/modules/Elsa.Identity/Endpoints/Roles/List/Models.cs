namespace Elsa.Identity.Endpoints.Roles.List;

internal record RoleSummary(
    string Id,
    string Name,
    ICollection<string> Permissions,
    string? TenantId
);

internal record Response(ICollection<RoleSummary> Roles);

