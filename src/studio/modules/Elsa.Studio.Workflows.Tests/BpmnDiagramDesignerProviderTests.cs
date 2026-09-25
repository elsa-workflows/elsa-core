using System.Text.Json.Nodes;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.DiagramDesigners.Fallback;
using Elsa.Studio.Workflows.DiagramDesigners.Flowcharts;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.UI.Services;
using Microsoft.Extensions.Localization;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnDiagramDesignerProvider"/>: it claims an <c>Elsa.BpmnProcess</c> root and
/// nothing else, and once registered, the diagram designer service picks it over the fallback for a
/// BPMN root.
/// </summary>
public class BpmnDiagramDesignerProviderTests
{
    private readonly BpmnDiagramDesignerProvider _provider = new(new TestLocalizer(), Microsoft.Extensions.Options.Options.Create(new DesignerOptions()), null!, null!, null!, null!);

    [Fact]
    public void GetSupportsActivity_ReturnsTrueForBpmnProcessActivity()
    {
        var activity = CreateActivity("Elsa.BpmnProcess");

        Assert.True(_provider.GetSupportsActivity(activity));
    }

    [Theory]
    [InlineData("Elsa.Flowchart")]
    [InlineData("Elsa.WriteLine")]
    [InlineData("Elsa.StateMachine")]
    public void GetSupportsActivity_ReturnsFalseForNonBpmnActivities(string type)
    {
        var activity = CreateActivity(type);

        Assert.False(_provider.GetSupportsActivity(activity));
    }

    [Fact]
    public void GetEditor_ReturnsBpmnDiagramDesigner()
    {
        var editor = _provider.GetEditor();

        Assert.IsType<BpmnDiagramDesigner>(editor);
    }

    [Fact]
    public void DiagramDesignerService_ChoosesTheBpmnProviderOverTheFallback_ForABpmnRoot()
    {
        var service = new DefaultDiagramDesignerService([_provider, new FallbackDesignerProvider()]);
        var activity = CreateActivity("Elsa.BpmnProcess");

        Assert.True(service.HasDiagramDesigner(activity));
        Assert.IsType<BpmnDiagramDesigner>(service.GetDiagramDesigner(activity));
    }

    [Fact]
    public void DiagramDesignerService_StillChoosesTheFlowchartProvider_ForAFlowchartRoot()
    {
        var flowchartProvider = new FlowchartDiagramDesignerProvider(new TestLocalizer(), null!, Microsoft.Extensions.Options.Options.Create(new DesignerOptions()));
        var service = new DefaultDiagramDesignerService([_provider, flowchartProvider, new FallbackDesignerProvider()]);
        var activity = CreateActivity("Elsa.Flowchart");

        Assert.IsType<FlowchartDiagramDesigner>(service.GetDiagramDesigner(activity));
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
