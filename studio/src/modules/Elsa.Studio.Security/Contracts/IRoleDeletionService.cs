using Elsa.Studio.Security.Models;

namespace Elsa.Studio.Security.Contracts;

/// <summary>
/// Coordinates versioned role deletion and the optional editable-policy remediation flow.
/// </summary>
public interface IRoleDeletionService
{
    Task<RoleDeletionInspectionResult> InspectAsync(
        string roleId,
        RoleAdministrationAccess access,
        CancellationToken cancellationToken = default);

    Task<RoleDeletionOperationResult> DeleteAsync(
        string roleId,
        RoleAdministrationAccess access,
        CancellationToken cancellationToken = default);

    Task<RoleDeletionOperationResult> RemediateAndDeleteAsync(
        string roleId,
        RoleAdministrationAccess access,
        RoleDeletionConfirmation confirmation,
        CancellationToken cancellationToken = default);
}
