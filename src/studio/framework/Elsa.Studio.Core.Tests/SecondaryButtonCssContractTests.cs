using System.Reflection;
using MudBlazor;
using Xunit;

namespace Elsa.Studio.Core.Tests;

public sealed class SecondaryButtonCssContractTests
{
    private const string SecondaryButtonSelector =
        ".mud-button-root.mud-button-outlined.mud-button-outlined-default";

    private static readonly string[] SecondaryButtonTokens =
    [
        "--elsa-secondary-button-surface",
        "--elsa-secondary-button-surface-hover",
        "--elsa-secondary-button-border",
        "--elsa-secondary-button-border-hover"
    ];

    [Fact]
    public void NeutralOutlinedButtonsUseSoftThemeAwareInteractionStates()
    {
        var css = CssContractTestContext.ReadRepositoryFile(
            "src", "framework", "Elsa.Studio.Shell", "wwwroot", "css", "shell.css");

        foreach (var token in SecondaryButtonTokens)
            Assert.Contains($"{token}:", css, StringComparison.Ordinal);

        var button = CssContractTestContext.GetRuleBody(css, SecondaryButtonSelector);
        var hover = CssContractTestContext.GetRuleBody(
            css,
            $"{SecondaryButtonSelector}:not(.mud-disabled):not([disabled]):hover");
        var focus = CssContractTestContext.GetRuleBody(
            css,
            $"{SecondaryButtonSelector}:not(.mud-disabled):not([disabled]):focus-visible");
        var disabled = CssContractTestContext.GetRuleBody(
            css,
            $"{SecondaryButtonSelector}:is(.mud-disabled, [disabled])");

        Assert.Contains("background: var(--elsa-secondary-button-surface)", button, StringComparison.Ordinal);
        Assert.Contains("border-color: var(--elsa-secondary-button-border)", button, StringComparison.Ordinal);
        Assert.Contains("text-transform: none", button, StringComparison.Ordinal);
        Assert.Contains("background: var(--elsa-secondary-button-surface-hover)", hover, StringComparison.Ordinal);
        Assert.Contains("border-color: var(--elsa-secondary-button-border-hover)", hover, StringComparison.Ordinal);
        Assert.Contains("color: var(--mud-palette-primary)", hover, StringComparison.Ordinal);
        Assert.Contains("outline: 2px solid color-mix(", focus, StringComparison.Ordinal);
        Assert.Contains("outline-offset: 2px", focus, StringComparison.Ordinal);
        Assert.Contains("background: var(--mud-palette-action-disabled-background)", disabled, StringComparison.Ordinal);
        Assert.Contains("border-color: transparent", disabled, StringComparison.Ordinal);
    }

    [Fact]
    public void MudBlazorMarksDefaultOutlinedButtonsForTheSharedTreatment()
    {
        var className = GetOutlinedButtonClassName(Color.Default);

        Assert.Contains("mud-button-outlined-default", className, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(Color.Primary, "primary")]
    [InlineData(Color.Secondary, "secondary")]
    [InlineData(Color.Tertiary, "tertiary")]
    [InlineData(Color.Info, "info")]
    [InlineData(Color.Success, "success")]
    [InlineData(Color.Warning, "warning")]
    [InlineData(Color.Error, "error")]
    [InlineData(Color.Dark, "dark")]
    [InlineData(Color.Inherit, "inherit")]
    public void MudBlazorKeepsSemanticOutlinedButtonsOutsideSharedTreatment(Color color, string colorClass)
    {
        var className = GetOutlinedButtonClassName(color);

        Assert.Contains($"mud-button-outlined-{colorClass}", className, StringComparison.Ordinal);
        Assert.DoesNotContain("mud-button-outlined-default", className, StringComparison.Ordinal);
    }

    private static string GetOutlinedButtonClassName(Color color)
    {
        var button = new MudButton();
        typeof(MudButton).GetProperty(nameof(MudButton.Variant))!.SetValue(button, Variant.Outlined);
        typeof(MudButton).GetProperty(nameof(MudButton.Color))!.SetValue(button, color);
        var classNameProperty = typeof(MudButton).GetProperty(
            "Classname",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(classNameProperty);
        return Assert.IsType<string>(classNameProperty.GetValue(button));
    }
}
