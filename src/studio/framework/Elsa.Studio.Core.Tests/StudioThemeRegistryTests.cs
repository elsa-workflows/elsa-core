using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor;
using Xunit;

namespace Elsa.Studio.Core.Tests;

public class StudioThemeRegistryTests
{
    [Fact]
    public void Selects_human_automation_when_no_theme_is_configured()
    {
        var services = new ServiceCollection();
        services.AddCoreInternal();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IStudioThemeRegistry>();

        Assert.Equal(StudioThemeIds.HumanAutomation, registry.Selected.Id);
        Assert.NotNull(registry.ResolveSelectedProvider().GetTheme());
    }

    [Fact]
    public void Selects_the_canonical_registration_for_a_case_insensitive_configuration_value()
    {
        var services = new ServiceCollection();
        services.AddCoreInternal(options => options.Theme = "WORKFLOW-AURORA");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IStudioThemeRegistry>();

        Assert.Equal(StudioThemeIds.WorkflowAurora, registry.Selected.Id);
    }

    [Fact]
    public void Rejects_an_unknown_configured_theme()
    {
        var services = new ServiceCollection();
        services.AddCoreInternal(options => options.Theme = "missing");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var exception = Assert.Throws<OptionsValidationException>(
            () => scope.ServiceProvider.GetRequiredService<IStudioThemeRegistry>());

        Assert.Contains("no matching studio theme", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Preserves_a_pre_registered_custom_theme_provider()
    {
        var expectedTheme = new MudTheme
        {
            PaletteLight = { Primary = new("1d4ed8") }
        };
        var services = new ServiceCollection();
        services.AddScoped<IThemeProvider>(_ => new TestThemeProvider(expectedTheme));
        services.AddCoreInternal();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var themeService = scope.ServiceProvider.GetRequiredService<IThemeService>();

        Assert.Same(expectedTheme, themeService.CurrentTheme);
    }

    [Fact]
    public void Switches_to_the_dark_palette_when_dark_mode_changes()
    {
        var services = new ServiceCollection();
        services.AddCoreInternal();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var themeService = scope.ServiceProvider.GetRequiredService<IThemeService>();
        var darkModeChanged = false;
        themeService.IsDarkModeChanged += () => darkModeChanged = true;

        themeService.IsDarkMode = true;

        Assert.True(darkModeChanged);
        Assert.Equal(
            themeService.CurrentTheme.PaletteDark.Background,
            themeService.CurrentPalette.Background);
        Assert.NotEqual(
            themeService.CurrentTheme.PaletteLight.Background,
            themeService.CurrentPalette.Background);
    }

    private sealed class TestThemeProvider(MudTheme theme) : IThemeProvider
    {
        public MudTheme GetTheme() => theme;
    }
}
