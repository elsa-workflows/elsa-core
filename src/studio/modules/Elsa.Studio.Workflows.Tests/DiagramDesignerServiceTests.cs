using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.DiagramDesigners.Fallback;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.UI.Contexts;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using Elsa.Studio.Workflows.UI.Services;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public class DiagramDesignerServiceTests
{
    private readonly DefaultDiagramDesignerService _service = new([
        new TestDiagramDesignerProvider(),
        new FallbackDesignerProvider()
    ]);

    [Fact]
    public void HasDiagramDesigner_ReturnsTrueForDedicatedProvider()
    {
        var activity = new JsonObject { ["type"] = "Test.Container" };

        Assert.True(_service.HasDiagramDesigner(activity));
    }

    [Fact]
    public void HasDiagramDesigner_DoesNotCountFallbackProvider()
    {
        var activity = new JsonObject { ["type"] = "Test.Activity" };

        Assert.False(_service.HasDiagramDesigner(activity));
        Assert.IsType<FallbackDiagramDesigner>(_service.GetDiagramDesigner(activity));
    }

    private sealed class TestDiagramDesignerProvider : IDiagramDesignerProvider
    {
        public double Priority => 10;
        public bool GetSupportsActivity(JsonObject activity) => activity["type"]?.GetValue<string>() == "Test.Container";
        public IDiagramDesigner GetEditor() => new TestDiagramDesigner();
    }

    private sealed class TestDiagramDesigner : IDiagramDesigner
    {
        public Task LoadRootActivityAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStatsMap) => Task.CompletedTask;
        public Task UpdateActivityAsync(string id, JsonObject activity) => Task.CompletedTask;
        public Task UpdateActivityStatsAsync(string id, ActivityStats stats) => Task.CompletedTask;
        public Task SelectActivityAsync(string id) => Task.CompletedTask;
        public Task<JsonObject> ReadRootActivityAsync() => Task.FromResult(new JsonObject());
        public RenderFragment DisplayDesigner(DisplayContext context) => _ => { };
    }
}
