using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Copilot.Adapters;
using GitHub.Copilot;
using Microsoft.Extensions.AI;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Elsa.AI.Copilot.UnitTests;

public class ElsaCopilotToolFunctionTests
{
    [Test]
    [DisplayName("Tool function preserves Elsa metadata and schema")]
    public async Task ToolFunctionPreservesElsaMetadataAndSchema()
    {
        var definition = CreateDefinition();
        var function = new ElsaCopilotToolFunction(definition, new CapturingToolInvoker());

        await Assert.That(function.Name).IsEqualTo("workflow.inspect");
        await Assert.That(function.Description).IsEqualTo("Inspect workflow");
        await Assert.That(function.JsonSchema.GetProperty("required")[0].GetString()).IsEqualTo("workflowId");
        var additionalProperties = function.AdditionalProperties!;

        await Assert.That(additionalProperties["elsa_mutability"]).IsEqualTo("Proposal");
        await Assert.That(additionalProperties["elsa_danger_level"]).IsEqualTo("Medium");
        await Assert.That((bool)additionalProperties["skip_permission"]!).IsFalse();
    }

    [Test]
    [DisplayName("Tool function invokes Elsa provider tool invoker")]
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
        await Assert.That(result).IsOfType(typeof(ToolResultAIContent));
        var toolResult = (ToolResultAIContent)result!;

        await Assert.That(toolResult.Result.TextResultForLlm).IsEqualTo("Workflow inspected");
        var invocation = await Assert.That(invoker.Invocations).HasSingleItem();
        await Assert.That(invocation.Id).IsEqualTo("sdk-tool-call-1");
        await Assert.That(invocation.ToolName).IsEqualTo("workflow.inspect");
        await Assert.That(invocation.Arguments["workflowId"]!.GetValue<string>()).IsEqualTo("workflow-1");
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
