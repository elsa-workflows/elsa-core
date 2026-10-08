using System.Reflection;
using System.Text.Json;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using GitHub.Copilot;
using Microsoft.Extensions.AI;

namespace Elsa.AI.Copilot.Adapters;

public class ElsaCopilotToolFunction(AIToolDefinition definition, IAIProviderToolInvoker toolInvoker) : AIFunction
{
    private static readonly MethodInfo InvokeMethod = typeof(ElsaCopilotToolFunction).GetMethod(nameof(InvokeToolAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly JsonElement _jsonSchema = JsonSerializer.SerializeToElement(definition.Schema, SerializerOptions);
    private readonly IReadOnlyDictionary<string, object?> _additionalProperties = CreateAdditionalProperties(definition);

    public override string Name => definition.Name;
    public override string Description => definition.Description;
    public override JsonElement JsonSchema => _jsonSchema;
    public override JsonSerializerOptions JsonSerializerOptions => SerializerOptions;
    public override MethodInfo UnderlyingMethod => InvokeMethod;
    public override IReadOnlyDictionary<string, object?> AdditionalProperties => _additionalProperties;

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var result = await InvokeToolAsync(arguments, cancellationToken);
        return new ToolResultAIContent(new ToolResultObject
        {
            TextResultForLlm = result.Summary,
            ResultType = result.Status == AIToolInvocationStatus.Failed ? "error" : "text",
            Error = result.Error,
            ToolTelemetry = new Dictionary<string, object>
            {
                ["status"] = result.Status.ToString(),
                ["toolName"] = definition.Name
            }
        });
    }

    private async ValueTask<AIToolResult> InvokeToolAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var invocation = new AIProviderToolInvocation
        {
            Id = ReadToolCallId(arguments) ?? Guid.NewGuid().ToString("N"),
            ToolName = definition.Name,
            Arguments = ToJsonObject(arguments)
        };

        return await toolInvoker.InvokeAsync(invocation, cancellationToken);
    }

    private static string? ReadToolCallId(AIFunctionArguments arguments)
    {
        if (arguments.Context == null)
            return null;

        foreach (var value in arguments.Context.Values)
        {
            if (value is ToolInvocation invocation && !string.IsNullOrWhiteSpace(invocation.ToolCallId))
                return invocation.ToolCallId;
        }

        return null;
    }

    private static JsonObject ToJsonObject(AIFunctionArguments arguments)
    {
        var json = new JsonObject();
        foreach (var (key, value) in arguments)
            json[key] = value == null ? null : JsonSerializer.SerializeToNode(value, SerializerOptions);

        return json;
    }

    private static IReadOnlyDictionary<string, object?> CreateAdditionalProperties(AIToolDefinition definition)
    {
        var properties = new Dictionary<string, object?>
        {
            ["elsa_mutability"] = definition.Mutability.ToString(),
            ["elsa_danger_level"] = definition.DangerLevel.ToString(),
            ["skip_permission"] = definition.Mutability == AIToolMutability.ReadOnly && definition.DangerLevel == AIToolDangerLevel.Low
        };

        if (!string.IsNullOrWhiteSpace(definition.Provider))
            properties["elsa_provider"] = definition.Provider;

        return properties;
    }
}
