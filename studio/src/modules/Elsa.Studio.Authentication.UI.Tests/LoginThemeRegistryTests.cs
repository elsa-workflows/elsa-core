using Elsa.Studio.Authentication.UI.Contracts;
using Elsa.Studio.Authentication.UI.Extensions;
using Elsa.Studio.Authentication.UI.Models;
using Elsa.Studio.Authentication.UI.Options;
using Elsa.Studio.Authentication.UI.Services;
using Elsa.Studio.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.UI.Tests;

public class LoginThemeRegistryTests
{
    [Fact]
    public void Selects_the_canonical_registration_for_a_case_insensitive_configuration_value()
    {
        var services = CreateServices("WORKFLOW-AURORA");
        services.AddLoginThemeProvider<TestLoginThemeProvider>("workflow-aurora");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<ILoginThemeRegistry>();

        Assert.Equal("workflow-aurora", registry.Selected.Id);
        Assert.IsType<TestLoginThemeProvider>(registry.ResolveSelectedProvider());
    }

    [Fact]
    public void Selects_the_application_theme_when_login_is_configured_to_inherit()
    {
        var services = CreateServices(LoginThemeIds.Inherit);
        services.Configure<StudioThemeOptions>(options => options.Theme = "human-automation");
        services.AddLoginThemeProvider<TestLoginThemeProvider>("human-automation");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<ILoginThemeRegistry>();

        Assert.Equal("human-automation", registry.Selected.Id);
    }

    [Fact]
    public void Throws_for_duplicate_registrations_without_hiding_them()
    {
        var services = CreateServices("classic");
        services.AddLoginThemeProvider<TestLoginThemeProvider>("classic");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var exception = Assert.Throws<OptionsValidationException>(
            () => scope.ServiceProvider.GetRequiredService<ILoginThemeRegistry>());

        Assert.Contains("registered more than once", exception.Message);
    }

    [Fact]
    public void Throws_for_an_unknown_selected_theme()
    {
        var services = CreateServices("missing");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var exception = Assert.Throws<OptionsValidationException>(
            () => scope.ServiceProvider.GetRequiredService<ILoginThemeRegistry>());

        Assert.Contains("no matching login theme", exception.Message);
    }

    [Fact]
    public void AddAuthenticationUI_registers_each_classic_presentation_once_when_called_more_than_once()
    {
        var services = new ServiceCollection();
        services.AddAuthenticationUI();
        services.AddAuthenticationUI();

        var registrations = services
            .Where(x => x.ServiceType == typeof(LoginThemeRegistration))
            .Select(x => Assert.IsType<LoginThemeRegistration>(x.ImplementationInstance))
            .ToArray();

        Assert.Equal(4, registrations.Length);
        Assert.Contains(registrations, x => x.Id == LoginThemeIds.Classic);
        Assert.Contains(registrations, x => x.Id == LoginThemeIds.ClassicUnified);
        Assert.Contains(registrations, x => x.Id == LoginThemeIds.ClassicRefinedSplit);
        Assert.Contains(registrations, x => x.Id == LoginThemeIds.ClassicBrandCanvas);
        Assert.Equal(
            registrations.Single(x => x.Id == LoginThemeIds.Classic).ProviderType,
            registrations.Single(x => x.Id == LoginThemeIds.ClassicUnified).ProviderType);
        Assert.NotEqual(
            registrations.Single(x => x.Id == LoginThemeIds.Classic).ProviderType,
            registrations.Single(x => x.Id == LoginThemeIds.ClassicRefinedSplit).ProviderType);
        Assert.NotEqual(
            registrations.Single(x => x.Id == LoginThemeIds.ClassicRefinedSplit).ProviderType,
            registrations.Single(x => x.Id == LoginThemeIds.ClassicBrandCanvas).ProviderType);
    }

    [Theory]
    [InlineData(LoginThemeIds.ClassicUnified)]
    [InlineData(LoginThemeIds.ClassicRefinedSplit)]
    [InlineData(LoginThemeIds.ClassicBrandCanvas)]
    public void Selects_each_named_classic_presentation(string theme)
    {
        var services = CreateServices(theme);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<ILoginThemeRegistry>();

        Assert.Equal(theme, registry.Selected.Id);
    }

    [Fact]
    public void AddAuthenticationUI_does_not_hide_a_pre_registered_classic_theme()
    {
        var services = new ServiceCollection();
        services.AddLoginThemeProvider<TestLoginThemeProvider>("classic");
        services.AddAuthenticationUI();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var exception = Assert.Throws<OptionsValidationException>(
            () => scope.ServiceProvider.GetRequiredService<ILoginThemeRegistry>());

        Assert.Contains("registered more than once", exception.Message);
    }

    private static ServiceCollection CreateServices(string theme)
    {
        var services = new ServiceCollection();
        services.AddAuthenticationUI();
        services.Configure<LoginThemeOptions>(options => options.Theme = theme);
        return services;
    }
}
