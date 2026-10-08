using Elsa.Studio.Security.Components;
using Elsa.Studio.Security.Models;
using MudBlazor;

namespace Elsa.Studio.Security.Services;

/// <summary>Opens the shared role-deletion workflow with consistent parameters and presentation.</summary>
public static class RoleDeletionDialogLauncher
{
    public static async Task<DeleteRoleDialogResult?> ShowRoleDeletionAsync(
        this IDialogService dialogService,
        RoleSummary role,
        RoleAdministrationAccess access,
        IReadOnlyCollection<RoleSummary>? replacementRoles = null)
    {
        var parameters = new DialogParameters<DeleteRoleDialog>
        {
            { x => x.RoleId, role.Id },
            { x => x.RoleName, role.Name },
            { x => x.Access, access },
            { x => x.ReplacementRoles, replacementRoles ?? [] }
        };
        var options = new DialogOptions
        {
            CloseOnEscapeKey = false,
            CloseButton = false,
            FullWidth = true,
            MaxWidth = MaxWidth.Medium
        };

        var reference = await dialogService.ShowAsync<DeleteRoleDialog>("Delete role", parameters, options);
        var result = await reference.Result;
        return result is { Canceled: false, Data: DeleteRoleDialogResult deletionResult }
            ? deletionResult
            : null;
    }
}
