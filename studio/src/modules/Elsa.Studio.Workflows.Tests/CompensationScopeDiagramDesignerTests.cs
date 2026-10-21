using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.DiagramDesigners.CompensationScopes;
using Elsa.Studio.Workflows.UI.Contexts;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public class CompensationScopeDiagramDesignerTests
{
    [Fact]
    public async Task UpdateActivityAsync_ReplacingRoot_NotifiesAfterReplacement()
    {
        var scope = CreateScope("before");
        var designer = await CreateDesignerAsync(scope);
        var callbackCount = 0;
        JsonObject? rootSeenByCallback = null;

        async Task CaptureUpdatedRootAsync()
        {
            callbackCount++;
            rootSeenByCallback = await designer.ReadRootActivityAsync();
        }

        designer.DisplayDesigner(new DisplayContext(scope, GraphUpdatedCallback: CreateGraphUpdatedCallback(CaptureUpdatedRootAsync)));

        var replacement = CreateScope("after");
        replacement["name"] = "Updated scope";

        await designer.UpdateActivityAsync("scope-1", replacement);

        Assert.Equal(1, callbackCount);
        var observedRoot = Assert.IsType<JsonObject>(rootSeenByCallback);
        Assert.Equal("Updated scope", observedRoot["name"]!.GetValue<string>());
        Assert.Equal("after", GetStepChild(observedRoot, "forward")["text"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("forward", "forward-1")]
    [InlineData("handler", "handler-1")]
    public async Task UpdateActivityAsync_ReplacingOwnedChild_NotifiesAfterReplacement(string role, string activityId)
    {
        var scope = CreateScope("before");
        var designer = await CreateDesignerAsync(scope);
        var callbackCount = 0;
        JsonObject? rootSeenByCallback = null;

        async Task CaptureUpdatedRootAsync()
        {
            callbackCount++;
            rootSeenByCallback = await designer.ReadRootActivityAsync();
        }

        designer.DisplayDesigner(new DisplayContext(scope, GraphUpdatedCallback: CreateGraphUpdatedCallback(CaptureUpdatedRootAsync)));
        var replacement = (JsonObject)GetStepChild(scope, role).DeepClone();
        replacement["text"] = "after";

        await designer.UpdateActivityAsync(activityId, replacement);

        Assert.Equal(1, callbackCount);
        var observedChild = GetStepChild(Assert.IsType<JsonObject>(rootSeenByCallback), role);
        Assert.Equal("after", observedChild["text"]!.GetValue<string>());
        Assert.Equal("literal payload", observedChild["inputs"]!["text"]!["value"]!["payload"]!.GetValue<string>());
        Assert.Equal("metadata payload", observedChild["metadata"]!["example"]!["value"]!.GetValue<string>());
    }

    [Fact]
    public async Task UpdateActivityAsync_ActivityShapedInputAndMetadata_DoNotMatchOwnedChildren()
    {
        var scope = CreateScope("before");
        var original = scope.DeepClone();
        var designer = await CreateDesignerAsync(scope);
        var callbackCount = 0;
        designer.DisplayDesigner(new DisplayContext(scope, GraphUpdatedCallback: CreateGraphUpdatedCallback(() =>
        {
            callbackCount++;
            return Task.CompletedTask;
        })));

        await designer.UpdateActivityAsync("literal-data-activity", new JsonObject
        {
            ["id"] = "literal-data-activity",
            ["type"] = "Elsa.WriteLine",
            ["text"] = "replacement"
        });
        Assert.Equal(0, callbackCount);
        Assert.True(JsonNode.DeepEquals(original, await designer.ReadRootActivityAsync()));

        await designer.UpdateActivityAsync("metadata-data-activity", new JsonObject
        {
            ["id"] = "metadata-data-activity",
            ["type"] = "Elsa.WriteLine",
            ["text"] = "replacement"
        });
        Assert.Equal(0, callbackCount);
        Assert.True(JsonNode.DeepEquals(original, await designer.ReadRootActivityAsync()));
    }

    private static async Task<CompensationScopeDiagramDesigner> CreateDesignerAsync(JsonObject scope)
    {
        var designer = new CompensationScopeDiagramDesigner();
        await designer.LoadRootActivityAsync(scope, null);
        return designer;
    }

    private static EventCallback CreateGraphUpdatedCallback(Func<Task> callback) =>
        EventCallback.Factory.Create(new object(), callback);

    private static JsonObject CreateScope(string text) => new()
    {
        ["id"] = "scope-1",
        ["type"] = "Elsa.CompensationScope",
        ["name"] = "Scope",
        ["steps"] = new JsonArray
        {
            new JsonObject
            {
                ["id"] = "step-1",
                ["forward"] = CreateWriteLine("forward-1", text),
                ["handler"] = CreateWriteLine("handler-1", "handler before")
            }
        }
    };

    private static JsonObject CreateWriteLine(string id, string text) => new()
    {
        ["id"] = id,
        ["type"] = "Elsa.WriteLine",
        ["version"] = 1,
        ["text"] = text,
        ["inputs"] = new JsonObject
        {
            ["text"] = new JsonObject
            {
                ["type"] = "Literal",
                ["value"] = new JsonObject
                {
                    ["id"] = "literal-data-activity",
                    ["type"] = "Elsa.WriteLine",
                    ["version"] = 1,
                    ["payload"] = "literal payload"
                }
            }
        },
        ["metadata"] = new JsonObject
        {
            ["example"] = new JsonObject
            {
                ["id"] = "metadata-data-activity",
                ["type"] = "Elsa.WriteLine",
                ["version"] = 1,
                ["value"] = "metadata payload"
            }
        }
    };

    private static JsonObject GetStepChild(JsonObject scope, string role) =>
        scope["steps"]!.AsArray()[0]!.AsObject()[role]!.AsObject();
}
