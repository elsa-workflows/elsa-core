using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Domain.Services;
using Elsa.Studio.Workflows.UI.Models;
using Elsa.Studio.Workflows.UI.Providers;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers the marker an <c>Elsa.BpmnProcess</c> node carries wherever it is composed. Without settings of its own it
/// falls back to the registry's generic cube and the platform's primary colour -- which renders perfectly well and
/// tells the reader nothing, so the failure is invisible on the canvas and has to be asserted here.
/// </summary>
public class BpmnProcessDisplaySettingsTests
{
    private readonly DefaultActivityDisplaySettingsRegistry _registry = new([new DefaultActivityDisplaySettingsProvider()]);

    [Fact]
    public void ABpmnProcess_HasAMarkerOfItsOwn()
    {
        var settings = _registry.GetSettings(BpmnProcessConstants.ActivityTypeName);
        var fallback = _registry.GetSettings("Some.Activity.Without.Settings");

        Assert.NotEqual(fallback.Icon, settings.Icon);
        Assert.NotEqual(fallback.Color, settings.Color);
    }

    [Fact]
    public void ABpmnProcess_IsNotColouredAsAFlowchart()
    {
        var settings = _registry.GetSettings(BpmnProcessConstants.ActivityTypeName);

        Assert.Equal(DefaultActivityColors.Bpmn, settings.Color);
        Assert.NotEqual(DefaultActivityColors.Flowchart, settings.Color);
    }
}
