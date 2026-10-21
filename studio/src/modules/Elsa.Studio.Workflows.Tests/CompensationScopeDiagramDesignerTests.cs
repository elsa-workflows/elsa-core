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
        Assert.Equal("after", GetForwardActivity(observedRoot)["text"]!.GetValue<string>());
    }

    [Fact]
    public async Task UpdateActivityAsync_ReplacingChild_NotifiesAfterReplacement()
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
        var replacement = new JsonObject
        {
            ["id"] = "write-line-1",
            ["type"] = "Elsa.WriteLine",
            ["text"] = "after"
        };

        await designer.UpdateActivityAsync("write-line-1", replacement);

        Assert.Equal(1, callbackCount);
        var observedRoot = Assert.IsType<JsonObject>(rootSeenByCallback);
        Assert.Equal("after", GetForwardActivity(observedRoot)["text"]!.GetValue<string>());
    }

    [Fact]
    public async Task UpdateActivityAsync_UnknownChild_DoesNotNotifyOrChangeTheRoot()
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

        await designer.UpdateActivityAsync("missing-activity", new JsonObject
        {
            ["id"] = "missing-activity",
            ["type"] = "Elsa.WriteLine",
            ["text"] = "after"
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
                ["forwardActivity"] = new JsonObject
                {
                    ["id"] = "write-line-1",
                    ["type"] = "Elsa.WriteLine",
                    ["text"] = text
                }
            }
        }
    };

    private static JsonObject GetForwardActivity(JsonObject scope) =>
        scope["steps"]!.AsArray()[0]!.AsObject()["forwardActivity"]!.AsObject();
}
