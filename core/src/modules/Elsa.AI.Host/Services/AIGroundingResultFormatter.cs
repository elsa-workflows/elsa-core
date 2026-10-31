using System.Text.Json;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Options;
using Microsoft.Extensions.Options;

namespace Elsa.AI.Host.Services;

public class AIGroundingResultFormatter(IOptions<AIHostOptions> options)
{
    private static readonly string[] SensitiveNameFragments =
    [
        "password",
        "secret",
        "token",
        "apikey",
        "api_key",
        "authorization",
        "connectionstring",
        "connection_string",
        "credential"
    ];

    public AIToolResult CreateResult(string summary, IEnumerable<JsonObject> items, int total, IEnumerable<string>? evidence = null, IEnumerable<string>? warnings = null)
    {
        var maxItems = Math.Max(0, options.Value.Grounding.MaxItems);
        var itemList = items
            .Take(maxItems)
            .Select(x => RedactObject((JsonObject)x.DeepClone()))
            .ToList();
        var truncated = total > itemList.Count;
        var groundingResult = new AIGroundingToolResult
        {
            Summary = summary,
            Items = itemList,
            Total = total,
            Returned = itemList.Count,
            Truncated = truncated,
            Evidence = evidence?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [],
            Warnings = warnings?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? []
        };

        var data = AIGroundingJson.ToJsonObject(groundingResult);
        return new AIToolResult
        {
            Summary = truncated ? $"{summary} Returned {itemList.Count} of {total} results." : summary,
            Data = LimitData(data)
        };
    }

    public AIToolResult Unavailable(string sourceName) =>
        new()
        {
            Status = AIToolInvocationStatus.Failed,
            Summary = $"{sourceName} is not available in this Elsa host.",
            Error = $"{sourceName} is not available.",
            Data = new JsonObject
            {
                ["available"] = false,
                ["source"] = sourceName
            }
        };

    public JsonObject RedactObject(JsonObject source)
    {
        var redacted = new JsonObject();
        foreach (var (key, value) in source)
            redacted[key] = IsSensitiveKey(key) ? "***" : RedactNode(value);

        return redacted;
    }

    public JsonObject LimitData(JsonObject data)
    {
        var maxBytes = Math.Max(0, options.Value.Grounding.MaxResultBytes);
        if (maxBytes == 0 || JsonSerializer.SerializeToUtf8Bytes(data).Length <= maxBytes)
            return data;

        return new JsonObject
        {
            ["truncated"] = true,
            ["maxBytes"] = maxBytes
        };
    }

    private JsonNode? RedactNode(JsonNode? node) =>
        node switch
        {
            JsonObject jsonObject => RedactObject(jsonObject),
            JsonArray jsonArray => RedactArray(jsonArray),
            null => null,
            _ => node.DeepClone()
        };

    private JsonArray RedactArray(JsonArray source)
    {
        var redacted = new JsonArray();
        foreach (var value in source)
            redacted.Add(RedactNode(value));

        return redacted;
    }

    private static bool IsSensitiveKey(string key)
    {
        var normalized = key.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
        return SensitiveNameFragments.Any(x => normalized.Contains(x, StringComparison.OrdinalIgnoreCase));
    }
}
