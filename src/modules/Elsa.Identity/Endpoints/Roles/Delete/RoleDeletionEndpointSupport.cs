using Elsa.Identity.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Identity.Endpoints.Roles.Delete;

internal static class RoleDeletionEndpointSupport
{
    public static Task SendOperationResultAsync(HttpContext context, RoleDeletionOperationResult result, CancellationToken cancellationToken) => result switch
    {
        RoleDeletionOperationResult.Deleted => SendNoContentAsync(context),
        RoleDeletionOperationResult.NotFound => SendErrorAsync(context, StatusCodes.Status404NotFound, "not_found", "The role was not found.", null, cancellationToken),
        RoleDeletionOperationResult.Forbidden => SendErrorAsync(context, StatusCodes.Status403Forbidden, "forbidden", "The caller may not delete this role or update all affected policies.", null, cancellationToken),
        RoleDeletionOperationResult.Blocked blocked => SendErrorAsync(context, StatusCodes.Status409Conflict, "conflict", "The role is referenced by one or more policies.", new { code = "role_referenced_by_jit_policy", deletionImpact = RoleDeletionImpactResponse.From(blocked.Impact) }, cancellationToken),
        RoleDeletionOperationResult.PreconditionFailed conflict => SendErrorAsync(context, StatusCodes.Status409Conflict, "conflict", "The role dependencies changed. Inspect the current impact and retry.", new { code = "role_dependency_changed", deletionImpact = RoleDeletionImpactResponse.From(conflict.Impact) }, cancellationToken),
        RoleDeletionOperationResult.ConfirmationRequired confirmation => SendErrorAsync(context, StatusCodes.Status400BadRequest, "confirmation_required", "Explicit confirmation is required.", new { warnings = confirmation.Warnings, deletionImpact = RoleDeletionImpactResponse.From(confirmation.Impact) }, cancellationToken),
        RoleDeletionOperationResult.Incomplete incomplete => SendErrorAsync(context, StatusCodes.Status409Conflict, "conflict", "Role-policy remediation did not complete; the role was not deleted.", new { code = "role_remediation_incomplete", reason = incomplete.Code, changedOwnerIds = incomplete.ChangedOwnerIds, deletionImpact = RoleDeletionImpactResponse.From(incomplete.Impact) }, cancellationToken),
        _ => throw new InvalidOperationException("Unknown role-deletion operation result.")
    };

    public static async Task SendInspectionResultAsync(HttpContext context, RoleDeletionInspectionResult result, CancellationToken cancellationToken)
    {
        switch (result)
        {
            case RoleDeletionInspectionResult.Success success:
                await context.Response.WriteAsJsonAsync(RoleDeletionImpactResponse.From(success.Impact), cancellationToken);
                break;
            case RoleDeletionInspectionResult.NotFound:
                await SendErrorAsync(context, StatusCodes.Status404NotFound, "not_found", "The role was not found.", null, cancellationToken);
                break;
            case RoleDeletionInspectionResult.Forbidden:
                await SendErrorAsync(context, StatusCodes.Status403Forbidden, "forbidden", "The caller may not inspect deletion impact for this role.", null, cancellationToken);
                break;
        }
    }

    private static Task SendNoContentAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return Task.CompletedTask;
    }

    private static Task SendErrorAsync(HttpContext context, int statusCode, string error, string message, object? details, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new RoleDeletionErrorResponse(error, message, details), cancellationToken);
    }
}
