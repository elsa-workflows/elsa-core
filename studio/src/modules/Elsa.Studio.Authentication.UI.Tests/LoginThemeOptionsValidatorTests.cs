using Elsa.Studio.Authentication.UI.Contracts;
using Elsa.Studio.Authentication.UI.Models;
using Elsa.Studio.Authentication.UI.Options;
using Elsa.Studio.Authentication.UI.Services;
using Elsa.Studio.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.UI.Tests;

public class LoginThemeOptionsValidatorTests
{
    [Fact]
    public void Accepts_default_classic_theme()
    {
        var result = CreateValidator(("classic", typeof(TestLoginThemeProvider)))
            .Validate(null, new LoginThemeOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Accepts_case_insensitive_theme_selection()
    {
        var result = CreateValidator(("workflow-aurora", typeof(TestLoginThemeProvider)))
            .Validate(null, new LoginThemeOptions { Theme = "WORKFLOW-AURORA" });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Resolves_inherited_theme_from_the_application_presentation()
    {
        var validator = new LoginThemeOptionsValidator(
            [new LoginThemeRegistration("human-automation", typeof(TestLoginThemeProvider))],
            Microsoft.Extensions.Options.Options.Create(
                new StudioThemeOptions { Theme = "HUMAN-AUTOMATION" }));

        var result = validator.Validate(
            null,
            new LoginThemeOptions { Theme = LoginThemeIds.Inherit });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Rejects_inherited_theme_without_a_matching_login_presentation()
    {
        var validator = new LoginThemeOptionsValidator(
            [new LoginThemeRegistration("classic", typeof(TestLoginThemeProvider))],
            Microsoft.Extensions.Options.Options.Create(
                new StudioThemeOptions { Theme = "human-automation" }));

        var result = validator.Validate(
            null,
            new LoginThemeOptions { Theme = LoginThemeIds.Inherit });

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Failures!,
            failure => failure.Contains("resolves to 'human-automation'", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Rejects_blank_theme_selection(string theme)
    {
        var result = CreateValidator(("classic", typeof(TestLoginThemeProvider)))
            .Validate(null, new LoginThemeOptions { Theme = theme });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("must not be blank", StringComparison.Ordinal));
    }

    [Fact]
    public void Rejects_unknown_theme_selection()
    {
        var result = CreateValidator(("classic", typeof(TestLoginThemeProvider)))
            .Validate(null, new LoginThemeOptions { Theme = "unknown" });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("no matching login theme", StringComparison.Ordinal));
    }

    [Fact]
    public void Rejects_duplicate_theme_identifiers()
    {
        var result = CreateValidator(
                ("classic", typeof(TestLoginThemeProvider)),
                ("CLASSIC", typeof(AnotherTestLoginThemeProvider)))
            .Validate(null, new LoginThemeOptions());

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("registered more than once", StringComparison.Ordinal));
    }

    [Fact]
    public void Rejects_invalid_provider_type()
    {
        var result = CreateValidator(("classic", typeof(string)))
            .Validate(null, new LoginThemeOptions());

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains(nameof(ILoginThemeProvider), StringComparison.Ordinal));
    }

    private static LoginThemeOptionsValidator CreateValidator(params (string Id, Type ProviderType)[] registrations) =>
        new(registrations.Select(x => new LoginThemeRegistration(x.Id, x.ProviderType)));
}
