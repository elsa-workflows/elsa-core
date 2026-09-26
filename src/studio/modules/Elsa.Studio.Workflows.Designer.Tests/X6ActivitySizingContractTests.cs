using Xunit;

namespace Elsa.Studio.Workflows.Designer.Tests;

public sealed class X6ActivitySizingContractTests
{
    [Fact]
    public void DetachedActivityMeasurements_UseTheLiveDesignerLayoutContext()
    {
        var calculation = ReadAsset("calculate-activity-size.ts");
        var update = ReadAsset("update-activity-size.ts");
        var graphCreation = ReadAsset("create-graph.ts");

        Assert.Contains("getActivityMeasurementScopeClass", calculation, StringComparison.Ordinal);
        Assert.Contains("wrapper.className = item.measurementScopeClass", calculation, StringComparison.Ordinal);
        Assert.Contains("calculateActivitySize(activity, portCount, measurementScopeClass)", update, StringComparison.Ordinal);
        Assert.Contains("enforceMinimumNodeSize(node, measurementScopeClass)", graphCreation, StringComparison.Ordinal);
    }

    [Fact]
    public void ActivitySizeCache_DistinguishesEffectiveLabelsAndDesignerLayouts()
    {
        var calculation = ReadAsset("calculate-activity-size.ts");

        Assert.Contains("activity.name", calculation, StringComparison.Ordinal);
        Assert.Contains("measurementScopeClass", calculation, StringComparison.Ordinal);
    }

    [Fact]
    public void ManualResize_EnforcesAndPersistsSizeOnlyAfterTheGestureCompletes()
    {
        var graphCreation = ReadAsset("create-graph.ts");

        Assert.Contains("graph.on('node:resized', onNodeResizeCompleted)", graphCreation, StringComparison.Ordinal);
        Assert.DoesNotContain("graph.on('node:change:size'", graphCreation, StringComparison.Ordinal);
    }

    private static string ReadAsset(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DesignerAssets", name));
}
