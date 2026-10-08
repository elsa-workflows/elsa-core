namespace Elsa.Identity.Endpoints.Users.List;

internal record UserSummary(
    string Id,
    string Name,
    ICollection<string> Roles,
    string? TenantId
);

internal record Response(ICollection<UserSummary> Users);

