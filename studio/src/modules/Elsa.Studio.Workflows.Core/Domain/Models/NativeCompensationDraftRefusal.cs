using System.Net;
using System.Text.Json;
using Elsa.Studio.Workflows.Domain.Extensions;

namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// A feature-owned view of the native compensation draft-only publish refusal.
/// </summary>
public sealed record NativeCompensationDraftRefusal(IReadOnlyList<NativeCompensationDraftDiagnostic> Diagnostics)
{
    public const string Code = "NATIVE_COMPENSATION_DRAFT_ONLY";

    private const int MaxDiagnostics = 200;
    private const int MaxRelatedIds = 100;
    private const int MaxIdentifierLength = 256;

    /// <summary>
    /// Parses only the known native compensation code. Unrecognized codes remain on the ordinary validation path.
    /// </summary>
    public static NativeCompensationDraftRefusal? FromValidationErrors(ValidationErrors errors)
    {
        if (!string.Equals(errors.Code, Code, StringComparison.Ordinal))
            return null;

        return new(ParseDiagnostics(errors.Data));
    }

    /// <summary>
    /// Preserves this feature's machine code when a native refusal has no message fields, which the generic parser
    /// intentionally declines to treat as a validation error. Unfamiliar codes keep their existing conversion path.
    /// </summary>
    public static ValidationErrors? TryGetValidationErrorsFromContent(string? content, HttpStatusCode? statusCode = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var parsed = ValidationApiExceptionExtensions.GetValidationErrorsFromContent(content);
        if (parsed != null && FromValidationErrors(parsed) != null)
            return parsed with { StatusCode = statusCode ?? parsed.StatusCode };

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !TryGetProperty(root, "code", out var code) ||
                code.ValueKind != JsonValueKind.String ||
                !string.Equals(code.GetString(), Code, StringComparison.Ordinal))
                return null;

            JsonElement? data = TryGetProperty(root, "data", out var dataElement) ? dataElement.Clone() : null;
            return new ValidationErrors([], statusCode, Code, data);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyList<NativeCompensationDraftDiagnostic> ParseDiagnostics(JsonElement? data)
    {
        if (data is not { ValueKind: JsonValueKind.Object } dataObject ||
            !TryGetProperty(dataObject, "diagnostics", out var diagnostics) ||
            diagnostics.ValueKind != JsonValueKind.Array)
            return [];

        var result = new List<NativeCompensationDraftDiagnostic>();
        foreach (var item in diagnostics.EnumerateArray().Take(MaxDiagnostics))
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var role = ReadRole(item);
            if (role == null)
                continue;

            var scopeActivityId = ReadIdentifier(item, "scopeActivityId");
            var stepId = ReadIdentifier(item, "stepId");
            var relatedActivityIds = ReadIdentifiers(item, "relatedActivityIds");
            var mappingIds = ReadIdentifiers(item, "mappingIds");
            var mappingId = ReadIdentifier(item, "mappingId");

            if (scopeActivityId == null && stepId == null && relatedActivityIds.Count == 0 && mappingId == null && mappingIds.Count == 0)
                continue;

            result.Add(new(scopeActivityId, stepId, role.Value, relatedActivityIds, mappingIds, mappingId));
        }

        return result;
    }

    private static NativeCompensationDraftDiagnosticRole? ReadRole(JsonElement diagnostic)
    {
        if (!TryGetProperty(diagnostic, "role", out var role) || role.ValueKind != JsonValueKind.String)
            return null;

        return role.GetString() switch
        {
            "scope" => NativeCompensationDraftDiagnosticRole.Scope,
            "forward" => NativeCompensationDraftDiagnosticRole.Forward,
            "handler" => NativeCompensationDraftDiagnosticRole.Handler,
            "mapping" => NativeCompensationDraftDiagnosticRole.Mapping,
            _ => null
        };
    }

    private static string? ReadIdentifier(JsonElement element, string propertyName)
    {
        if (!TryGetProperty(element, propertyName, out var value) || value.ValueKind != JsonValueKind.String)
            return null;

        var identifier = value.GetString();
        return !string.IsNullOrWhiteSpace(identifier) && identifier.Length <= MaxIdentifierLength ? identifier : null;
    }

    private static IReadOnlyList<string> ReadIdentifiers(JsonElement element, string propertyName)
    {
        if (!TryGetProperty(element, propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
            return [];

        return values.EnumerateArray()
            .Take(MaxRelatedIds)
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x) && x.Length <= MaxIdentifierLength)
            .Select(x => x!)
            .ToArray();
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var candidate in element.EnumerateObject())
            {
                if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    property = candidate.Value;
                    return true;
                }
            }
        }

        property = default;
        return false;
    }
}

/// <summary>
/// The role names accepted from the native compensation diagnostic envelope.
/// </summary>
public enum NativeCompensationDraftDiagnosticRole
{
    Scope,
    Forward,
    Handler,
    Mapping
}

/// <summary>
/// A validated diagnostic association containing only stable identifiers and an allow-listed role.
/// </summary>
public sealed record NativeCompensationDraftDiagnostic(
    string? ScopeActivityId,
    string? StepId,
    NativeCompensationDraftDiagnosticRole Role,
    IReadOnlyList<string> RelatedActivityIds,
    IReadOnlyList<string> MappingIds,
    string? MappingId);
