using System.Text.Json.Nodes;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;

namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Identifies the server contract required to safely author and save native compensation drafts.
/// </summary>
public static class NativeCompensationDraftCapability
{
    public const string CompensationScopeActivityTypeName = "Elsa.CompensationScope";
    public const string RemoteFeatureName = "Elsa.NativeCompensationDraftAuthoringV1";
    public const string CompensatedHandoff = "Compensated";
    public const string PropagateOriginalFaultHandoff = "PropagateOriginalFault";

    public static bool ContainsCompensationScope(JsonNode? node)
    {
        return node is JsonObject activity &&
               (IsCompensationScopeActivity(GetString(activity, "type")) || IsOpaqueCompensationScope(activity));
    }

    public static bool IsOpaqueCompensationScope(JsonObject activity)
    {
        if (GetString(activity, "type") != "Elsa.NotFoundActivity")
            return false;

        return GetString(activity, "missingTypeName") == "Elsa.CompensationScope" ||
               activity["originalActivityJson"] is JsonValue originalValue &&
               originalValue.TryGetValue<string>(out var originalJson) &&
               ContainsCompensationScopeActivityRoot(originalJson);
    }

    public static bool ContainsUnsupportedCompensationScopeShape(JsonNode? node)
    {
        return node is JsonObject activity &&
               (IsOpaqueCompensationScope(activity) ||
                IsCompensationScopeActivity(GetString(activity, "type")) && !HasSupportedDraftShape(activity));
    }

    /// <summary>
    /// Cheap marker precheck only. A positive result means the contextual graph classifier must inspect
    /// the document; it does not establish that the marker is an activity edge.
    /// </summary>
    public static bool ContainsPotentialScopeMarker(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            if (IsCompensationScopeActivity(GetString(obj, "type")) ||
                string.Equals(GetString(obj, "type"), "Elsa.NotFoundActivity", StringComparison.Ordinal) &&
                IsCompensationScopeActivity(GetString(obj, "missingTypeName")))
                return true;

            if (string.Equals(GetString(obj, "type"), "Elsa.NotFoundActivity", StringComparison.Ordinal) &&
                TryParseOriginalActivity(obj, out var original) && ContainsPotentialScopeMarker(original))
                return true;

            return obj.Any(property => ContainsPotentialScopeMarker(property.Value));
        }

        return node is JsonArray array && array.Any(ContainsPotentialScopeMarker);
    }

    public static async Task<bool> IsEnabledAsync(IRemoteFeatureProvider? provider, CancellationToken cancellationToken = default) =>
        provider is IRemoteFeatureProviderWithFreshCheck freshProvider &&
        await freshProvider.IsEnabledFreshOrDefaultAsync(RemoteFeatureName, cancellationToken);

    public static async Task<bool> IsEnabledForContextAsync(
        IRemoteFeatureProvider? provider,
        IBackendApiClientProvider backendApiClientProvider,
        string expectedBackendContextKey,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(backendApiClientProvider.ContextKey, expectedBackendContextKey, StringComparison.Ordinal))
            return false;

        var enabled = await IsEnabledAsync(provider, cancellationToken);
        return enabled &&
               backendApiClientProvider is IBackendRequestContextBinding { SupportsAuthenticatedRequestContextBinding: true } &&
               string.Equals(backendApiClientProvider.ContextKey, expectedBackendContextKey, StringComparison.Ordinal);
    }

    public static bool IsCompensationScopeActivity(string? activityTypeName) =>
        string.Equals(activityTypeName, CompensationScopeActivityTypeName, StringComparison.Ordinal);

    public static Task<bool> IsActivityCreationEnabledForDocumentAsync(
        string? activityTypeName,
        IRemoteFeatureProvider? provider,
        IBackendApiClientProvider backendApiClientProvider,
        string? expectedBackendContextKey,
        CancellationToken cancellationToken = default)
    {
        if (!IsCompensationScopeActivity(activityTypeName))
            return Task.FromResult(true);

        if (string.IsNullOrWhiteSpace(expectedBackendContextKey))
            return Task.FromResult(false);

        return IsEnabledForContextAsync(provider, backendApiClientProvider, expectedBackendContextKey, cancellationToken);
    }

    public static bool HasSupportedDraftShape(JsonNode? node)
    {
        if (node is not JsonObject scope ||
            GetString(scope, "type") != "Elsa.CompensationScope" ||
            GetInt32(scope, "version") != 1 ||
            GetInt32(scope, "draftSchemaVersion") != 1 ||
            scope["steps"] is not JsonArray steps ||
            GetString(scope, "onForwardFault") is not (null or CompensatedHandoff or PropagateOriginalFaultHandoff))
            return false;

        var stepIds = new HashSet<string>(StringComparer.Ordinal);
        return steps.All(step => step is JsonObject row &&
                                 GetString(row, "stepId") is { } stepId &&
                                 !string.IsNullOrWhiteSpace(stepId) &&
                                 stepIds.Add(stepId) &&
                                 (!row.TryGetPropertyValue("mappings", out var mappings) || mappings is JsonArray));
    }

    private static bool ContainsCompensationScopeActivityRoot(string json)
    {
        try
        {
            return JsonNode.Parse(json) is JsonObject activity &&
                   (IsCompensationScopeActivity(GetString(activity, "type")) || IsOpaqueCompensationScope(activity));
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    internal static bool TryParseOriginalActivity(JsonObject activity, out JsonNode? original)
    {
        original = null;
        if (activity["originalActivityJson"] is not JsonValue value || !value.TryGetValue<string>(out var json))
            return false;

        try
        {
            original = JsonNode.Parse(json);
            return original != null;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private static string? GetString(JsonObject value, string propertyName) =>
        value[propertyName] is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var result) ? result : null;

    private static int? GetInt32(JsonObject value, string propertyName) =>
        value[propertyName] is JsonValue jsonValue && jsonValue.TryGetValue<int>(out var result) ? result : null;
}
