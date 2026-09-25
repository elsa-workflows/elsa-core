using Xunit;

namespace Elsa.Studio.Workflows.Designer.Tests;

public sealed class X6SequenceRenderingContractTests
{
    [Fact]
    public void SequenceLayout_MutationsNotifyX6SoNodesAndEdgesAreRedrawn()
    {
        var sequenceMode = ReadAsset("sequence-mode.ts");
        var graphCreation = ReadAsset("create-graph.ts");

        Assert.Contains("graph.batchUpdate('sequence-layout'", sequenceMode, StringComparison.Ordinal);
        Assert.Contains("sequenceLayout: true", sequenceMode, StringComparison.Ordinal);
        Assert.DoesNotContain("silent: true", sequenceMode, StringComparison.Ordinal);
        Assert.Contains("args.options?.sequenceLayout", graphCreation, StringComparison.Ordinal);
    }

    [Fact]
    public void HorizontalSequenceLayout_UsesMeasuredNodeSpacingAndSideAnchors()
    {
        var sequenceMode = ReadAsset("sequence-mode.ts");

        Assert.Contains("node.getSize()", sequenceMode, StringComparison.Ordinal);
        Assert.Contains("+ SequenceNodeGap", sequenceMode, StringComparison.Ordinal);
        Assert.Contains("Math.max(SequenceMinimumStride", sequenceMode, StringComparison.Ordinal);
        Assert.Contains("horizontal ? 'right' : 'bottom'", sequenceMode, StringComparison.Ordinal);
        Assert.Contains("horizontal ? 'left' : 'top'", sequenceMode, StringComparison.Ordinal);
    }

    [Fact]
    public void SequenceLayout_ReflowsAfterProgrammaticAndCompletedManualSizeChanges()
    {
        var activitySizing = ReadAsset("update-activity-size.ts");
        var graphCreation = ReadAsset("create-graph.ts");

        Assert.Contains("graphBinding.mode === 'sequence'", activitySizing, StringComparison.Ordinal);
        Assert.Contains("arrangeSequenceGraph(graphBinding);", activitySizing, StringComparison.Ordinal);
        Assert.Contains("if (isSequenceMode)\n            arrangeSequenceGraph(binding);", graphCreation, StringComparison.Ordinal);
        Assert.Contains("graph.on('node:resized', onNodeResizeCompleted);", graphCreation, StringComparison.Ordinal);
        Assert.DoesNotContain("graph.on('node:change:size'", graphCreation, StringComparison.Ordinal);
    }

    private static string ReadAsset(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DesignerAssets", name));
}
