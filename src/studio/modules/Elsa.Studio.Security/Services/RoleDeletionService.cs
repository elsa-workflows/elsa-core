using System.Text.Json;
using Elsa.Studio.Contracts;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;
using Refit;

namespace Elsa.Studio.Security.Services;

/// <summary>
/// Coordinates role deletion against the versioned Core impact contract.
/// </summary>
public sealed class RoleDeletionService(IBackendApiClientProvider apiClientProvider) : IRoleDeletionService
{
    public async Task<RoleDeletionInspectionResult> InspectAsync(
        string roleId,
        RoleAdministrationAccess access,
        CancellationToken cancellationToken = default)
    {
        if (!access.CanDelete)
            return ForbiddenInspection();

        try
        {
            var rolesApi = await apiClientProvider.GetApiAsync<IRolesApi>(cancellationToken);
            var impact = await rolesApi.GetDeletionImpactAsync(roleId, cancellationToken);
            return ClassifyInspection(impact);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return MapInspectionException(exception);
        }
    }

    public async Task<RoleDeletionOperationResult> DeleteAsync(
        string roleId,
        RoleAdministrationAccess access,
        CancellationToken cancellationToken = default)
    {
        if (!access.CanDelete)
            return ForbiddenOperation();

        try
        {
            var rolesApi = await apiClientProvider.GetApiAsync<IRolesApi>(cancellationToken);
            await rolesApi.DeleteAsync(roleId, cancellationToken);
            return new() { Outcome = RoleDeletionOperationOutcome.Deleted };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return MapOperationException(exception);
        }
    }

    public async Task<RoleDeletionOperationResult> RemediateAndDeleteAsync(
        string roleId,
        RoleAdministrationAccess access,
        RoleDeletionConfirmation confirmation,
        CancellationToken cancellationToken = default)
    {
        if (!access.CanDelete)
            return ForbiddenOperation();

        try
        {
            var rolesApi = await apiClientProvider.GetApiAsync<IRolesApi>(cancellationToken);
            await rolesApi.RemediateAndDeleteAsync(
                roleId,
                new RoleRemediationRequest
                {
                    ExpectedDependencyVersion = confirmation.ExpectedDependencyVersion,
                    ConfirmRemoveFromEditableJitPolicies = confirmation.ConfirmRemoveFromEditableJitPolicies,
                    ConfirmEmptyDefaultRoles = confirmation.ConfirmEmptyDefaultRoles,
                    ConfirmBestEffort = confirmation.ConfirmBestEffort,
                    SelectedReferences = confirmation.SelectedReferences,
                    ReplacementRoleId = confirmation.ReplacementDefaultRoleId
                },
                cancellationToken);
            return new() { Outcome = RoleDeletionOperationOutcome.Deleted };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return MapOperationException(exception);
        }
    }

    private static RoleDeletionInspectionResult ClassifyInspection(RoleDeletionImpactResponse impact)
    {
        if (impact.ConfigurationReferences.Count != 0)
            return new() { Outcome = RoleDeletionInspectionOutcome.Blocked, Impact = impact };

        if (impact.EditableReferences.Count != 0)
        {
            return new()
            {
                Outcome = impact.CanRemediate
                    ? RoleDeletionInspectionOutcome.RemediationRequired
                    : RoleDeletionInspectionOutcome.Blocked,
                Impact = impact
            };
        }

        if (impact.Warnings.Count != 0)
        {
            return new()
            {
                Outcome = RoleDeletionInspectionOutcome.ConfirmationRequired,
                Impact = impact,
                Warnings = impact.Warnings
            };
        }

        return new()
        {
            Outcome = impact.CanDelete
                ? RoleDeletionInspectionOutcome.Safe
                : RoleDeletionInspectionOutcome.Blocked,
            Impact = impact
        };
    }

    private static RoleDeletionInspectionResult MapInspectionException(Exception exception)
    {
        var error = IdentityApiErrorMapper.Describe(exception);
        return new()
        {
            Outcome = error.IsAuthorization
                ? RoleDeletionInspectionOutcome.Forbidden
                : error.IsNotFound
                    ? RoleDeletionInspectionOutcome.NotFound
                    : RoleDeletionInspectionOutcome.Error,
            Code = error.Code,
            Message = error.Message
        };
    }

    private static RoleDeletionOperationResult MapOperationException(Exception exception)
    {
        var error = IdentityApiErrorMapper.Describe(exception);
        var payload = TryReadPayload(exception);
        var code = payload.Code ?? error.Code;
        var impact = payload.Impact;

        if (error.IsAuthorization)
            return new() { Outcome = RoleDeletionOperationOutcome.Forbidden, Code = code, Message = error.Message };

        if (error.IsNotFound)
            return new() { Outcome = RoleDeletionOperationOutcome.NotFound, Code = code, Message = error.Message };

        if (string.Equals(code, "role_dependency_changed", StringComparison.Ordinal) ||
            (error.IsConflict && impact is not null && string.Equals(code, "conflict", StringComparison.Ordinal)))
        {
            return new()
            {
                Outcome = RoleDeletionOperationOutcome.DependencyConflict,
                Impact = impact,
                Code = code,
                Message = error.Message
            };
        }

        if (string.Equals(code, "role_remediation_incomplete", StringComparison.Ordinal))
        {
            return new()
            {
                Outcome = RoleDeletionOperationOutcome.Incomplete,
                Impact = impact,
                Code = code,
                Message = error.Message,
                ChangedOwnerIds = payload.ChangedOwnerIds
            };
        }

        if (string.Equals(code, "role_referenced_by_jit_policy", StringComparison.Ordinal))
        {
            return new()
            {
                Outcome = RoleDeletionOperationOutcome.Blocked,
                Impact = impact,
                Code = code,
                Message = error.Message
            };
        }

        if (string.Equals(code, "confirmation_required", StringComparison.Ordinal))
        {
            return new()
            {
                Outcome = RoleDeletionOperationOutcome.ConfirmationRequired,
                Impact = impact,
                Code = code,
                Message = error.Message,
                Warnings = payload.Warnings
            };
        }

        if (error.IsConflict)
        {
            return new()
            {
                Outcome = RoleDeletionOperationOutcome.DependencyConflict,
                Impact = impact,
                Code = code,
                Message = error.Message,
                Warnings = payload.Warnings
            };
        }

        return new() { Outcome = RoleDeletionOperationOutcome.Error, Code = code, Message = error.Message };
    }

    private static RoleDeletionInspectionResult ForbiddenInspection() => new()
    {
        Outcome = RoleDeletionInspectionOutcome.Forbidden,
        Code = "forbidden",
        Message = "You are not allowed to delete this role."
    };

    private static RoleDeletionOperationResult ForbiddenOperation() => new()
    {
        Outcome = RoleDeletionOperationOutcome.Forbidden,
        Code = "forbidden",
        Message = "You are not allowed to delete this role."
    };

    private static RoleDeletionErrorPayload TryReadPayload(Exception exception)
    {
        if (exception is not ApiException apiException || string.IsNullOrWhiteSpace(apiException.Content))
            return RoleDeletionErrorPayload.Empty;

        try
        {
            using var document = JsonDocument.Parse(apiException.Content);
            var root = document.RootElement;
            var details = root.TryGetProperty("details", out var detailsElement) && detailsElement.ValueKind == JsonValueKind.Object
                ? detailsElement
                : root;

            var code = TryGetString(details, "code") ?? TryGetString(root, "code");
            var impactElement = TryGetObject(details, "deletionImpact") ?? TryGetObject(root, "deletionImpact");
            var impact = impactElement is { } element
                ? JsonSerializer.Deserialize(element.GetRawText(), IdentityJsonSerializerContext.Default.RoleDeletionImpactResponse)
                : null;
            var changedOwnerIds = TryGetStringArray(details, "changedOwnerIds") ?? TryGetStringArray(root, "changedOwnerIds") ?? [];
            var warnings = TryGetStringArray(details, "warnings") ?? TryGetStringArray(root, "warnings") ?? [];

            return new(code, impact, warnings, changedOwnerIds);
        }
        catch (JsonException)
        {
            return RoleDeletionErrorPayload.Empty;
        }
    }

    private static string? TryGetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static JsonElement? TryGetObject(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Object
            ? property
            : null;

    private static IReadOnlyCollection<string>? TryGetStringArray(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Array
            ? property.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToArray()
            : null;

    private sealed record RoleDeletionErrorPayload(
        string? Code,
        RoleDeletionImpactResponse? Impact,
        IReadOnlyCollection<string> Warnings,
        IReadOnlyCollection<string> ChangedOwnerIds)
    {
        public static RoleDeletionErrorPayload Empty { get; } = new(null, null, [], []);
    }
}
