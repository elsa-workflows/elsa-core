using System.Security.Claims;
using Elsa.Abstractions;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.ExternalAuthentication.Endpoints.Connections;

internal static class ConnectionEndpointSupport
{
    public static bool TryGetExpectedRevision(HttpContext context, out long revision)
    {
        revision = default;
        var value = context.Request.Headers.IfMatch.FirstOrDefault();
        return !string.IsNullOrWhiteSpace(value) && value.Length >= 2 && value[0] == '"' && value[^1] == '"' && long.TryParse(value[1..^1], out revision) && revision > 0;
    }

    public static void SetEtag(HttpContext context, long revision) => context.Response.Headers.ETag = $"\"{revision}\"";

    public static Task SendMutationResultAsync(HttpContext context, ManagementConnectionMutationResult result, IdentityProviderConnectionManagementService management, CancellationToken cancellationToken) => result switch
    {
        ManagementConnectionMutationResult.NotFound => SendErrorAsync(context, StatusCodes.Status404NotFound, "not_found", "The connection was not found.", cancellationToken),
        ManagementConnectionMutationResult.Forbidden => SendErrorAsync(context, StatusCodes.Status403Forbidden, "forbidden", "The connection cannot be mutated from the current tenant context.", cancellationToken),
        ManagementConnectionMutationResult.Conflict conflict => SendErrorAsync(context, StatusCodes.Status409Conflict, "conflict", "The requested connection change conflicts with current state.", new { code = conflict.Code }, cancellationToken),
        ManagementConnectionMutationResult.PreconditionFailed conflict => SendErrorAsync(context, StatusCodes.Status412PreconditionFailed, "precondition_failed", "The connection has changed. Reload it before trying again.", new { currentRevision = conflict.CurrentRevision }, cancellationToken),
        ManagementConnectionMutationResult.ValidationFailed validation => SendErrorAsync(context, StatusCodes.Status400BadRequest, "validation_failed", "The connection is not valid for this operation.", new { errors = validation.Validation.Errors, warnings = validation.Validation.Warnings }, cancellationToken),
        _ => throw new InvalidOperationException("A successful connection mutation must be handled by the endpoint.")
    };

    public static Task SendErrorAsync(HttpContext context, int statusCode, string error, string message, object? details, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new ManagementErrorResponse(error, message, details), cancellationToken);
    }

    public static Task SendErrorAsync(HttpContext context, int statusCode, string error, string message, CancellationToken cancellationToken) => SendErrorAsync(context, statusCode, error, message, null, cancellationToken);

    public static bool HasPermission(ClaimsPrincipal user, string permission) => user.FindAll(PermissionNames.ClaimType).Any(x => x.Value == PermissionNames.All || x.Value == permission);
    public static bool RequiresPolicyManagement(ConnectionRequest request) => request.UnlinkedPolicy is not null || request.PermissionGrantSources is { Count: > 0 };
    public static bool IsDatabaseOwned(EffectiveIdentityProviderConnection connection) => connection.Ownership == ConnectionSourceOwnership.Database;
}
