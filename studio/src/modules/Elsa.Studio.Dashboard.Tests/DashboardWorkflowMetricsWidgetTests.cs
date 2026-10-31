using System.Reflection;
using Elsa.Studio.Workflows.Dashboard.Widgets;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using MudBlazor;
using Xunit;

namespace Elsa.Studio.Dashboard.Tests;

public sealed class DashboardWorkflowMetricsWidgetTests
{
    [Fact]
#pragma warning disable BL0006 // Inspecting the generated render tree is the regression seam for invalid browser element names.
    public void MetricContent_DoesNotRenderEmptyHtmlElementNames()
    {
        var componentType = typeof(DashboardWorkflowMetricsWidget);
        var metricType = componentType.GetNestedType("OperationalHealthMetric", BindingFlags.NonPublic)!;
        var metric = Activator.CreateInstance(metricType, "Running", "1", "Current active instances", "icon", Color.Info, null)!;
        var fragmentFactory = (Delegate)componentType
            .GetProperty("MetricContent", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(new DashboardWorkflowMetricsWidget())!;
        var fragment = (RenderFragment)fragmentFactory.DynamicInvoke(metric)!;
        var builder = new RenderTreeBuilder();

        fragment(builder);

        var frames = builder.GetFrames();
        Assert.DoesNotContain(frames.Array.Take(frames.Count), frame =>
            frame.FrameType == RenderTreeFrameType.Element && string.IsNullOrWhiteSpace(frame.ElementName));
    }
#pragma warning restore BL0006
}
