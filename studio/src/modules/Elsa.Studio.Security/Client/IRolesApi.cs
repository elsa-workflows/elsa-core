using Elsa.Studio.Security.Models;
using Refit;

namespace Elsa.Studio.Security.Client;

/// <summary>Internal Elsa Identity role management endpoints used by Studio.</summary>
public interface IRolesApi
{
    [Get("/identity/roles")]
    Task<ListRolesResponse> ListAsync(CancellationToken cancellationToken = default);

    [Post("/identity/roles")]
    Task<CreateRoleResponse> CreateAsync([Body] CreateRoleRequest request, CancellationToken cancellationToken = default);

    [Put("/identity/roles/{id}")]
    Task<UpdateRoleResponse> UpdateAsync(string id, [Body] UpdateRoleRequest request, CancellationToken cancellationToken = default);

    [Delete("/identity/roles/{id}")]
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    [Get("/identity/roles/{id}/deletion-impact")]
    Task<RoleDeletionImpactResponse> GetDeletionImpactAsync(string id, CancellationToken cancellationToken = default);

    [Post("/identity/roles/{id}/remove-from-jit-policies-and-delete")]
    Task RemediateAndDeleteAsync(string id, [Body] RoleRemediationRequest request, CancellationToken cancellationToken = default);
}
