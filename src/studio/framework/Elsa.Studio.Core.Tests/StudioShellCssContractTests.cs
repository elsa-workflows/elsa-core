using Xunit;

namespace Elsa.Studio.Core.Tests;

public sealed class StudioShellCssContractTests
{
    private const string ActionsSelector = ".studio-appbar__actions";

    [Fact]
    public void UtilitiesUseSoftThemeAwareInteractionStates()
    {
        var layout = CssContractTestContext.ReadRepositoryFile(
            "src", "framework", "Elsa.Studio.Shared", "Layouts", "MainLayout.razor");
        var css = CssContractTestContext.ReadRepositoryFile(
            "src", "framework", "Elsa.Studio.Shell", "wwwroot", "css", "shell.css");

        Assert.Contains("studio-appbar__actions", layout, StringComparison.Ordinal);

        var actions = CssContractTestContext.GetRuleBody(css, ActionsSelector);
        var utilities = CssContractTestContext.GetRuleBody(css, $"{ActionsSelector} :is(.mud-icon-button, .mud-button-root)");
        var languagePicker = CssContractTestContext.GetRuleBody(css, $"{ActionsSelector} .mud-button-outlined");
        var hover = CssContractTestContext.GetRuleBody(
            css,
            $"{ActionsSelector} :is(.mud-icon-button, .mud-button-root):not(.mud-disabled):not([disabled]):hover");
        var focus = CssContractTestContext.GetRuleBody(
            css,
            $"{ActionsSelector} :is(.mud-icon-button, .mud-button-root):focus-visible");

        Assert.Contains("display: flex", actions, StringComparison.Ordinal);
        Assert.Contains("gap: var(--elsa-space-1)", actions, StringComparison.Ordinal);
        Assert.Contains("color: var(--elsa-text-muted)", actions, StringComparison.Ordinal);
        Assert.Contains("color: var(--elsa-text-muted)", utilities, StringComparison.Ordinal);
        Assert.Contains("border-color: color-mix(", languagePicker, StringComparison.Ordinal);
        Assert.Contains("var(--elsa-border) 42%", languagePicker, StringComparison.Ordinal);
        Assert.Contains("background: color-mix(", hover, StringComparison.Ordinal);
        Assert.Contains("color: var(--elsa-text)", hover, StringComparison.Ordinal);
        Assert.Contains("outline: 2px solid color-mix(", focus, StringComparison.Ordinal);
        Assert.Contains("outline-offset: 2px", focus, StringComparison.Ordinal);
        Assert.DoesNotContain("[data-elsa-theme=\"classic\"] .studio-appbar__actions", css, StringComparison.Ordinal);
    }

    [Fact]
    public void NavigationGroupLabelsUseComfortableLogicalInset()
    {
        var css = CssContractTestContext.ReadRepositoryFile(
            "src", "framework", "Elsa.Studio.Shell", "wwwroot", "css", "shell.css");

        var groupLabel = CssContractTestContext.GetRuleBody(css, ".studio-nav__group-label");

        Assert.Contains("padding-block: var(--elsa-space-6) var(--elsa-space-2)", groupLabel, StringComparison.Ordinal);
        Assert.Contains("padding-inline: var(--elsa-space-4) var(--elsa-space-3)", groupLabel, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Elsa.Studio.Login", "Components", "LoginState.razor")]
    [InlineData("Elsa.Studio.Environments", "Components", "EnvironmentPicker.razor")]
    [InlineData("Elsa.Studio.ExternalAuthentication.BlazorServer", "Components", "BrokerLoginState.razor")]
    [InlineData("Elsa.Studio.ExternalAuthentication.BlazorWasm", "Components", "BrokerLoginState.razor")]
    public void AccountMenusInheritShellUtilityColor(params string[] componentPath)
    {
        var path = new[] { "src", "modules" }.Concat(componentPath).ToArray();
        var component = CssContractTestContext.ReadRepositoryFile(path);

        Assert.Contains("IconColor=\"Color.Inherit\"", component, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Elsa.Studio.Host.Server", "Pages", "_Host.cshtml")]
    [InlineData("Elsa.Studio.Host.HostedWasm", "Pages", "_Host.cshtml")]
    public void ServerHostedShellStylesheetsUseContentHashVersioning(params string[] hostPath)
    {
        var path = new[] { "src", "hosts" }.Concat(hostPath).ToArray();
        var host = CssContractTestContext.ReadRepositoryFile(path);

        Assert.Contains(
            "href=\"_content/Elsa.Studio.Shell/css/shell.css\" rel=\"stylesheet\" asp-append-version=\"true\"",
            host,
            StringComparison.Ordinal);
    }
}
