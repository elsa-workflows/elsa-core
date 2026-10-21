using System.Text.Json.Nodes;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;

namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Identifies the server contract required to safely author and save native compensation drafts.
/// </summary>
public static class NativeCompensationDraftCapability
{
    public const string RemoteFeatureName = "Elsa.NativeCompensationDraftAuthoringV1";
    public const string CompensatedHandoff = "Compensated";
    public const string PropagateOriginalFaultHandoff = "PropagateOriginalFault";

    public static bool ContainsCompensationScope(JsonNode? node)
    {
        if (node is JsonObject activity)
        {
            if (GetString(activity, "type") == "Elsa.CompensationScope")
                return true;

            if (GetString(activity, "type") == "Elsa.NotFoundActivity")
            {
                if (GetString(activity, "missingTypeName") == "Elsa.CompensationScope")
                    return true;

                if (activity["originalActivityJson"] is JsonValue originalValue &&
                    originalValue.TryGetValue<string>(out var originalJson) &&
                    ContainsCompensationScopeJson(originalJson))
                    return true;
            }

            return activity.Any(x => ContainsCompensationScope(x.Value));
        }

        return node is JsonArray array && array.Any(ContainsCompensationScope);
    }

    public static async Task<bool> IsEnabledAsync(IRemoteFeatureProvider? provider, CancellationToken cancellationToken = default) =>
        provider is IRemoteFeatureProviderWithFreshCheck freshProvider &&
        await freshProvider.IsEnabledFreshOrDefaultAsync(RemoteFeatureName, cancellationToken);

    private static bool ContainsCompensationScopeJson(string json)
    {
        try
        {
            return JsonNode.Parse(json) is JsonObject original && GetString(original, "type") == "Elsa.CompensationScope";
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private static string? GetString(JsonObject value, string propertyName) =>
        value[propertyName] is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var result) ? result : null;
}
