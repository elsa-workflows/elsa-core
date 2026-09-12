using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Host.Tools;

public abstract class GroundingToolBase : IAITool
{
    public abstract AIToolDefinition Definition { get; }
    public abstract ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default);

    public void Dispose()
    {
    }

    protected static string? GetString(JsonObject arguments, string name) =>
        arguments.TryGetPropertyValue(name, out var node) && node is JsonValue value && value.TryGetValue<string>(out var result) && !string.IsNullOrWhiteSpace(result)
            ? result
            : null;

    protected static int? GetInt(JsonObject arguments, string name) =>
        arguments.TryGetPropertyValue(name, out var node) && node is JsonValue value && value.TryGetValue<int>(out var result)
            ? result
            : null;

    protected static bool? GetBool(JsonObject arguments, string name) =>
        arguments.TryGetPropertyValue(name, out var node) && node is JsonValue value && value.TryGetValue<bool>(out var result)
            ? result
            : null;

    protected static JsonObject? GetObject(JsonObject arguments, string name) =>
        arguments.TryGetPropertyValue(name, out var node) && node is JsonObject jsonObject
            ? jsonObject
            : null;

    protected static AIToolDefinition ReadOnlyDefinition(string name, string displayName, string description, JsonObject schema) =>
        new()
        {
            Name = name,
            DisplayName = displayName,
            Description = description,
            Schema = schema,
            Mutability = AIToolMutability.ReadOnly,
            DangerLevel = AIToolDangerLevel.Low
        };

    protected static AIToolDefinition ProposalDefinition(string name, string displayName, string description, JsonObject schema) =>
        new()
        {
            Name = name,
            DisplayName = displayName,
            Description = description,
            Schema = schema,
            Mutability = AIToolMutability.Proposal,
            DangerLevel = AIToolDangerLevel.Medium
        };
}
