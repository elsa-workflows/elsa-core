using System.Text.Json.Nodes;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.DiagramDesigners.StateMachines;
using Microsoft.Extensions.Localization;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public class StateMachineDiagramDesignerProviderTests
{
    private readonly StateMachineDiagramDesignerProvider _provider = new(new TestLocalizer());

    [Fact]
    public void GetSupportsActivity_ReturnsTrueForStateMachineActivity()
    {
        var activity = CreateActivity("Elsa.StateMachine");

        var result = _provider.GetSupportsActivity(activity);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Elsa.Flowchart")]
    [InlineData("Elsa.WriteLine")]
    public void GetSupportsActivity_ReturnsFalseForNonStateMachineActivities(string type)
    {
        var activity = CreateActivity(type);

        var result = _provider.GetSupportsActivity(activity);

        Assert.False(result);
    }

    [Fact]
    public void GetEditor_ReturnsStateMachineDesigner()
    {
        var editor = _provider.GetEditor();

        Assert.IsType<StateMachineDiagramDesigner>(editor);
    }

    [Fact]
    public async Task ReadRootActivityAsync_ReturnsLoadedActivityBeforeWrapperRenders()
    {
        var designer = new StateMachineDiagramDesigner(new TestLocalizer());
        var activity = CreateActivity("Elsa.StateMachine");

        await designer.LoadRootActivityAsync(activity, null);
        var result = await designer.ReadRootActivityAsync();

        Assert.Same(activity, result);
    }

    private static JsonObject CreateActivity(string type) => new()
    {
        ["type"] = type
    };

    private class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
