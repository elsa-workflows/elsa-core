using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Copilot.Adapters;
using GitHub.Copilot;
using Microsoft.Extensions.AI;
using System.Text.Json.Nodes;

namespace Elsa.AI.Copilot.UnitTests;

public class ElsaCopilotToolFunctionTests
{
    [Fact(DisplayName = "Tool function preserves Elsa metadata and schema")]
    public void ToolFunctionPreservesElsaMetadataAndSchema()
    {
        var definition = CreateDefinition();
        var function = new ElsaCopilotToolFunction(definition, new CapturingToolInvoker());

        Assert.Equal("workflow.inspect", function.Name);
        Assert.Equal("Inspect workflow", function.Description);
        Assert.Equal("workflowId", function.JsonSchema.GetProperty("required")[0].GetString());
        var additionalProperties = function.AdditionalProperties!;

        Assert.Equal("Proposal", additionalProperties["elsa_mutability"]);
        Assert.Equal("Medium", additionalProperties["elsa_danger_level"]);
        Assert.False((bool)additionalProperties["skip_permission"]!);
    }

    [Fact(DisplayName = "Tool function invokes Elsa provider tool invoker")]
    public async Task ToolFunctionInvokesElsaProviderToolInvoker()
    {
        var invoker = new CapturingToolInvoker();
        var function = new ElsaCopilotToolFunction(CreateDefinition(), invoker);
        var arguments = new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["workflowId"] = "workflow-1"
        })
        {
            Context = new Dictionary<object, object?>
            {
                ["copilotInvocation"] = new ToolInvocation
                {
                    ToolCallId = "sdk-tool-call-1"
                }
            }
        };

        var result = await function.InvokeAsync(arguments);
        var toolResult = Assert.IsType<ToolResultAIContent>(result);

        Assert.Equal("Workflow inspected", toolResult.Result.TextResultForLlm);
        var invocation = Assert.Single(invoker.Invocations);
        Assert.Equal("sdk-tool-call-1", invocation.Id);
        Assert.Equal("workflow.inspect", invocation.ToolName);
        Assert.Equal("workflow-1", invocation.Arguments["workflowId"]!.GetValue<string>());
    }

    private static AIToolDefinition CreateDefinition() =>
        new()
        {
            Name = "workflow.inspect",
            Description = "Inspect workflow",
            Mutability = AIToolMutability.Proposal,
            DangerLevel = AIToolDangerLevel.Medium,
            Schema = new JsonObject
            {
                ["type"] = "object",
                ["required"] = new JsonArray("workflowId"),
                ["properties"] = new JsonObject
                {
                    ["workflowId"] = new JsonObject
                    {
                        ["type"] = "string"
                    }
                }
            }
        };

    private class CapturingToolInvoker : IAIProviderToolInvoker
    {
        public List<AIProviderToolInvocation> Invocations { get; } = [];

        public ValueTask<AIToolResult> InvokeAsync(AIProviderToolInvocation invocation, CancellationToken cancellationToken = default)
        {
            Invocations.Add(invocation);
            return ValueTask.FromResult(new AIToolResult
            {
                Summary = "Workflow inspected"
            });
        }
    }
}
