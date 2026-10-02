using System.Text.Json.Nodes;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Designer.Options;
using Microsoft.Extensions.Localization;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnDiagramDesigner"/>'s read path: the designer is read-only in this
/// increment, so <see cref="BpmnDiagramDesigner.ReadRootActivityAsync"/> must hand back the exact
/// document it was given -- never a projection rebuilt from the canvas's own view model (D4) -- and
/// <see cref="BpmnDiagramDesigner.UpdateActivityAsync"/>'s only job is to keep that document in step
/// with an edit made elsewhere (the properties panel), without touching the canvas.
/// </summary>
public class BpmnDiagramDesignerTests
{
    private readonly BpmnDiagramDesigner _designer = new(new TestLocalizer(), Microsoft.Extensions.Options.Options.Create(new DesignerOptions()), null!, null!, null!, null!);

    [Fact]
    public async Task ReadRootActivityAsync_ReturnsTheLoadedActivityUnchanged()
    {
        var activity = LoadFixture();

        await _designer.LoadRootActivityAsync(activity, null);
        var result = await _designer.ReadRootActivityAsync();

        Assert.Same(activity, result);
        Assert.True(JsonNode.DeepEquals(activity, result));
    }

    [Fact]
    public async Task UpdateActivityAsync_ReplacesTheMatchingChildActivity_LeavingTheRestOfTheTreeAlone()
    {
        var activity = LoadFixture();
        await _designer.LoadRootActivityAsync(activity, null);

        var originalChild = activity["activities"]![0]!.AsObject();
        var childId = originalChild["id"]!.GetValue<string>();
        var updatedChild = new JsonObject
        {
            ["id"] = childId,
            ["type"] = "Elsa.WriteLine",
            ["text"] = "updated"
        };

        await _designer.UpdateActivityAsync(childId, updatedChild);
        var result = await _designer.ReadRootActivityAsync();

        var resultChild = result["activities"]![0]!.AsObject();
        Assert.Equal("updated", resultChild["text"]!.GetValue<string>());
        Assert.NotSame(updatedChild, resultChild);
    }

    [Fact]
    public async Task UpdateActivityAsync_ReplacesTheRootActivity_WhenTheIdMatchesTheRoot()
    {
        var activity = LoadFixture();
        await _designer.LoadRootActivityAsync(activity, null);

        var rootId = activity["id"]!.GetValue<string>();
        var updatedRoot = new JsonObject
        {
            ["id"] = rootId,
            ["type"] = "Elsa.BpmnProcess",
            ["name"] = "updated-root"
        };

        await _designer.UpdateActivityAsync(rootId, updatedRoot);
        var result = await _designer.ReadRootActivityAsync();

        Assert.Equal("updated-root", result["name"]!.GetValue<string>());
        Assert.NotSame(updatedRoot, result);
    }

    private static JsonObject LoadFixture()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "DesignerAssets", "camunda-order-process.activity.json");
        var json = File.ReadAllText(path);
        return JsonNode.Parse(json)!.AsObject();
    }

    private class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
