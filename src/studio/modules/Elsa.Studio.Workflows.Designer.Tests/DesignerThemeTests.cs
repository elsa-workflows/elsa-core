using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.Designer.Services;
using MudBlazor;
using Xunit;

namespace Elsa.Studio.Workflows.Designer.Tests;

public class DesignerThemeTests
{
    [Fact]
    public void FromPalette_ShouldMapEveryX6ColorFromTheActiveMudPalette()
    {
        var palette = new PaletteDark
        {
            Surface = "#18212b",
            BackgroundGray = "#233041",
            LinesDefault = "#405064",
            TextPrimary = "#f4f7fa",
            TextSecondary = "#aab6c4",
            Primary = "#66a9ff",
            Secondary = "#45c7b8",
            Success = "#65c98a"
        };

        var theme = X6DesignerTheme.FromPalette(palette);

        Assert.Equal("#aab6c46b", theme.Grid);
        Assert.Equal("#405064ff", theme.Edge);
        Assert.Equal("#18212bff", theme.PortSurface);
        Assert.Equal("#66a9ffff", theme.PortStroke);
        Assert.Equal("#aab6c4ff", theme.PortText);
        Assert.Equal("#66a9ffff", theme.Selection);
        Assert.Equal("#45c7b8ff", theme.ConnectionHighlight);
        Assert.Equal("#65c98aff", theme.EmbeddingHighlight);
    }

    [Fact]
    public void GridColor_ShouldDefaultToTheActiveTheme()
    {
        var gridArgs = new X6GridArgs();

        Assert.Null(gridArgs.Color);
    }

    [Fact]
    public void GridTheme_ShouldBePersistedAcrossX6ViewportUpdates()
    {
        var graphCreation = ReadAsset("create-graph.ts");
        var themeApplication = ReadAsset("apply-graph-theme.ts");
        var gridOptions = ReadAsset("grid-options.ts");

        Assert.Contains("gridOptions = createDesignerGridOptions(settings?.grid)", graphCreation, StringComparison.Ordinal);
        Assert.Contains("gridOptions,", graphCreation, StringComparison.Ordinal);
        Assert.Contains("graph.drawGrid(resolveDesignerGridOptions(gridOptions, theme.grid))", themeApplication, StringComparison.Ordinal);
        Assert.Contains("visible: settings?.visible ?? true", gridOptions, StringComparison.Ordinal);
        Assert.Contains("size: settings?.size ?? 10", gridOptions, StringComparison.Ordinal);
        Assert.Contains("item?.color ?? themeColor", gridOptions, StringComparison.Ordinal);
    }

    [Fact]
    public void ThemeSubscription_ShouldHandleBothThemeEventsAndUnsubscribe()
    {
        var themeService = new StubThemeService();
        var applyCount = 0;
        var subscription = new X6DesignerThemeSubscription(
            themeService,
            _ =>
            {
                applyCount++;
                return Task.CompletedTask;
            });

        themeService.RaiseCurrentThemeChanged();
        themeService.RaiseIsDarkModeChanged();

        Assert.Equal(2, applyCount);

        subscription.Dispose();
        themeService.RaiseCurrentThemeChanged();

        Assert.Equal(2, applyCount);
    }

    [Fact]
    public void V2NodeAssets_ShouldUseThemeTokensInsteadOfAWhiteInlineSurface()
    {
        var css = ReadAsset("designer.v2.css");
        var wrapper = ReadAsset("ActivityWrapper.razor");

        Assert.Contains("--elsa-designer-node-surface: var(--mud-palette-surface)", css);
        Assert.Contains("background-color: var(--elsa-designer-node-surface)", css);
        Assert.Contains("color: var(--elsa-designer-node-text)", css);
        Assert.Contains("color: var(--elsa-designer-node-muted)", css);
        Assert.Contains("--elsa-activity-accent", wrapper);
        Assert.DoesNotContain("const string white", wrapper);
        Assert.DoesNotContain("background-color: {backgroundColor}", wrapper);
    }

    [Fact]
    public void V1NodeAssets_ShouldAlsoRemainReadableInDarkMode()
    {
        var css = ReadAsset("designer.v1.css");
        var wrapper = ReadAsset("ActivityWrapperV1.razor");

        Assert.Contains("background-color: var(--mud-palette-surface)", css);
        Assert.Contains("color: var(--mud-palette-text-primary)", css);
        Assert.Contains("--elsa-activity-accent", wrapper);
        Assert.DoesNotContain("const string white", wrapper);
        Assert.DoesNotContain("background-color: {Color}", wrapper);
    }

    private static string ReadAsset(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DesignerAssets", name));

    private sealed class StubThemeService : IThemeService
    {
        public event Action CurrentThemeChanged = delegate { };
        public event Action IsDarkModeChanged = delegate { };
        public MudTheme CurrentTheme { get; set; } = new();
        public bool IsDarkMode { get; set; }

        public void RaiseCurrentThemeChanged() => CurrentThemeChanged();
        public void RaiseIsDarkModeChanged() => IsDarkModeChanged();
    }
}
