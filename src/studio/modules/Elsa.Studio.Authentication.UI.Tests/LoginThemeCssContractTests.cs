namespace Elsa.Studio.Authentication.UI.Tests;

public sealed class LoginThemeCssContractTests
{
    private static readonly string[] LoginMethodTokens =
    [
        "--elsa-login-method-surface",
        "--elsa-login-method-border",
        "--elsa-login-method-hover-surface",
        "--elsa-login-method-preferred-surface",
        "--elsa-login-method-preferred-border",
        "--elsa-login-method-preferred-color",
        "--elsa-login-method-icon-surface",
        "--elsa-login-method-icon-color",
        "--elsa-login-method-shadow"
    ];

    private static readonly string[] LoginControlTokens =
    [
        "--elsa-control-surface",
        "--elsa-control-surface-hover",
        "--elsa-control-surface-disabled",
        "--elsa-control-border",
        "--elsa-control-border-hover",
        "--elsa-control-focus"
    ];

    [Fact]
    public void LoginMethodTokens_ArePublicAndSupportedByBuiltInThemeFamilies()
    {
        var classicCss = ReadRepositoryFile(
            "src", "modules", "Elsa.Studio.Authentication.UI", "wwwroot", "css", "login.css");
        var readme = ReadRepositoryFile(
            "src", "modules", "Elsa.Studio.Authentication.UI", "README.md");
        var classicTheme = GetRuleText(classicCss, ".classic-login-theme");
        var modernTheme = GetRuleText(ReadModernThemeCss(), ".modern-login-theme");

        foreach (var token in LoginMethodTokens)
        {
            Assert.Contains(token, readme, StringComparison.Ordinal);
            Assert.Contains($"{token}:", classicTheme, StringComparison.Ordinal);
            Assert.Contains($"{token}:", modernTheme, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void LightModernTheme_OverridesTheCompleteLoginMethodPalette()
    {
        var humanAutomationTheme = GetRuleText(
            ReadModernThemeCss(),
            ".modern-login-theme[data-theme=\"human-automation\"]");

        foreach (var token in LoginMethodTokens)
            Assert.Contains($"{token}:", humanAutomationTheme, StringComparison.Ordinal);
    }

    [Fact]
    public void LoginThemes_OverrideTheSharedControlPalette()
    {
        var classicCss = ReadRepositoryFile(
            "src", "modules", "Elsa.Studio.Authentication.UI", "wwwroot", "css", "login.css");
        var modernCss = ReadModernThemeCss();
        var readme = ReadRepositoryFile(
            "src", "modules", "Elsa.Studio.Authentication.UI", "README.md");
        var classicTheme = GetRuleText(classicCss, ".classic-login-theme");
        var modernTheme = GetRuleText(modernCss, ".modern-login-theme");
        var humanAutomationTheme = GetRuleText(
            modernCss,
            ".modern-login-theme[data-theme=\"human-automation\"]");

        foreach (var token in LoginControlTokens)
        {
            Assert.Contains(token, readme, StringComparison.Ordinal);
            Assert.Contains($"{token}:", classicTheme, StringComparison.Ordinal);
            Assert.Contains($"{token}:", modernTheme, StringComparison.Ordinal);
        }

        foreach (var token in LoginControlTokens.Where(x => x != "--elsa-control-focus"))
            Assert.Contains($"{token}:", humanAutomationTheme, StringComparison.Ordinal);
    }

    [Fact]
    public void ClassicUnifiedTheme_UsesUnifiedIdentityGatewayComposition()
    {
        var classicCss = ReadRepositoryFile(
            "src", "modules", "Elsa.Studio.Authentication.UI", "wwwroot", "css", "login.css");
        var classicRazor = ReadRepositoryFile(
            "src", "modules", "Elsa.Studio.Authentication.UI", "Components", "Themes", "ClassicUnifiedLoginTheme.razor");
        var cardRule = GetRuleText(classicCss, ".classic-login-theme__card");

        Assert.Contains("classic-login-theme__handoff", classicRazor, StringComparison.Ordinal);
        Assert.Contains("classic-login-theme__footer", classicRazor, StringComparison.Ordinal);
        Assert.Contains("inline-size: min(100%, 32.5rem)", cardRule, StringComparison.Ordinal);
        Assert.DoesNotContain("grid-template-columns", cardRule, StringComparison.Ordinal);
    }

    [Fact]
    public void ClassicThemeFamily_ProvidesNamedRefinedSplitUnifiedAndBrandCanvasPresentations()
    {
        var classicCss = ReadRepositoryFile(
            "src", "modules", "Elsa.Studio.Authentication.UI", "wwwroot", "css", "login.css");
        var refinedSplitRazor = ReadRepositoryFile(
            "src", "modules", "Elsa.Studio.Authentication.UI", "Components", "Themes", "ClassicRefinedSplitLoginTheme.razor");
        var unifiedRazor = ReadRepositoryFile(
            "src", "modules", "Elsa.Studio.Authentication.UI", "Components", "Themes", "ClassicUnifiedLoginTheme.razor");
        var brandCanvasRazor = ReadRepositoryFile(
            "src", "modules", "Elsa.Studio.Authentication.UI", "Components", "Themes", "ClassicBrandCanvasLoginTheme.razor");

        Assert.Contains("classic-refined-split-login-theme__brand", refinedSplitRazor, StringComparison.Ordinal);
        Assert.Contains("classic-refined-split-login-theme__context", refinedSplitRazor, StringComparison.Ordinal);
        Assert.Contains(".classic-refined-split-login-theme__card", classicCss, StringComparison.Ordinal);
        Assert.Contains(".classic-refined-split-login-theme__panel", classicCss, StringComparison.Ordinal);
        Assert.Contains("classic-login-theme__handoff", unifiedRazor, StringComparison.Ordinal);
        Assert.Contains("classic-brand-canvas-login-theme__statement", brandCanvasRazor, StringComparison.Ordinal);
        Assert.Contains("classic-brand-canvas-login-theme__route", brandCanvasRazor, StringComparison.Ordinal);
        Assert.Contains(".classic-brand-canvas-login-theme__signin", classicCss, StringComparison.Ordinal);
        Assert.Contains(".classic-brand-canvas-login-theme__card", classicCss, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 820px)", classicCss, StringComparison.Ordinal);
    }

    [Fact]
    public void BuiltInCredentialLoginMethods_UseOutlinedDenseMudBlazorFields()
    {
        var components = new[]
        {
            ReadRepositoryFile(
                "src", "modules", "Elsa.Studio.Authentication.ElsaIdentity.UI", "Components", "ElsaIdentityLoginMethod.razor"),
            ReadRepositoryFile(
                "src", "modules", "Elsa.Studio.ExternalAuthentication", "Components", "LoginMethods", "BrokerLocalLoginMethod.razor")
        };

        foreach (var component in components)
        {
            var fields = System.Text.RegularExpressions.Regex.Matches(
                    component,
                    @"<MudTextField\b[\s\S]*?/>",
                    System.Text.RegularExpressions.RegexOptions.CultureInvariant)
                .Select(match => match.Value)
                .ToArray();

            Assert.NotEmpty(fields);
            Assert.All(fields, field =>
            {
                Assert.Contains("Variant=\"Variant.Outlined\"", field, StringComparison.Ordinal);
                Assert.Contains("Margin=\"Margin.Dense\"", field, StringComparison.Ordinal);
            });
        }
    }

    private static string ReadModernThemeCss() =>
        ReadRepositoryFile(
            "src", "modules", "Elsa.Studio.Authentication.Themes", "wwwroot", "css", "login-themes.css");

    private static string ReadRepositoryFile(params string[] pathSegments) =>
        File.ReadAllText(Path.Combine([FindRepositoryRoot(), .. pathSegments]));

    private static string GetRuleText(string css, string selector)
    {
        var start = css.IndexOf($"{selector} {{", StringComparison.Ordinal);
        Assert.True(start >= 0, $"CSS rule '{selector}' was not found.");
        var end = css.IndexOf('}', start);
        Assert.True(end >= 0, $"CSS rule '{selector}' is not closed.");
        return css[start..end];
    }

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src", "modules", "Elsa.Studio.Authentication.UI")))
                return current.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the Elsa Studio repository root.");
    }
}
