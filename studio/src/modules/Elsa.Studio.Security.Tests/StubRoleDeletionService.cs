using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;

namespace Elsa.Studio.Security.Tests;

internal sealed class StubRoleDeletionService : IRoleDeletionService
{
    public Task<RoleDeletionInspectionResult> InspectAsync(
        string roleId,
        RoleAdministrationAccess access,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new RoleDeletionInspectionResult
        {
            Outcome = RoleDeletionInspectionOutcome.Safe,
            Impact = new RoleDeletionImpactResponse { RoleId = roleId, DependencyVersion = "v1", CanDelete = true }
        });

    public Task<RoleDeletionOperationResult> DeleteAsync(
        string roleId,
        RoleAdministrationAccess access,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new RoleDeletionOperationResult { Outcome = RoleDeletionOperationOutcome.Deleted });

    public Task<RoleDeletionOperationResult> RemediateAndDeleteAsync(
        string roleId,
        RoleAdministrationAccess access,
        RoleDeletionConfirmation confirmation,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new RoleDeletionOperationResult { Outcome = RoleDeletionOperationOutcome.Deleted });
}
