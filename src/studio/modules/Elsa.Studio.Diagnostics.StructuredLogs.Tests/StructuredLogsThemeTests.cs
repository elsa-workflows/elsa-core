using Xunit;

namespace Elsa.Studio.Diagnostics.StructuredLogs.Tests;

public class StructuredLogsThemeTests
{
    [Fact]
    public void LogViewportAndDetailsUseActiveThemeSurfaces()
    {
        var css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "structuredLogs.css"));

        Assert.Contains("--structured-logs-viewport: color-mix(", css, StringComparison.Ordinal);
        Assert.Contains("background: var(--structured-logs-viewport)", css, StringComparison.Ordinal);
        Assert.Contains("background: var(--mud-palette-surface)", css, StringComparison.Ordinal);
        Assert.Contains("color: var(--mud-palette-text-primary)", css, StringComparison.Ordinal);
        Assert.Contains("border: 1px solid var(--mud-palette-lines-default)", css, StringComparison.Ordinal);
        Assert.DoesNotContain("background: #f8fafc", css, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("background: #fff", css, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LogRowsUseThemeAwareInteractionAndSeverityColors()
    {
        var css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "structuredLogs.css"));

        Assert.Contains("--structured-logs-row-hover: color-mix(", css, StringComparison.Ordinal);
        Assert.Contains("--structured-logs-row-selected: color-mix(", css, StringComparison.Ordinal);
        Assert.Contains("border-left-color: var(--mud-palette-warning)", css, StringComparison.Ordinal);
        Assert.Contains("border-left-color: var(--mud-palette-error)", css, StringComparison.Ordinal);
        Assert.Contains("color: var(--structured-logs-success-text)", css, StringComparison.Ordinal);
        Assert.Contains("color: var(--structured-logs-warning-text)", css, StringComparison.Ordinal);
        Assert.Contains("color: var(--structured-logs-error-text)", css, StringComparison.Ordinal);
    }
}
