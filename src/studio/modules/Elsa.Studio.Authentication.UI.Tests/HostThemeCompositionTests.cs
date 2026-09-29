using Elsa.Studio.Authentication.Themes.Extensions;
using Elsa.Studio.Authentication.UI.Contracts;
using Elsa.Studio.Authentication.UI.Extensions;
using Elsa.Studio.Authentication.UI.Models;
using Elsa.Studio.Authentication.UI.Options;
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorServer.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.UI.Tests;

public class HostThemeCompositionTests
{
    [Fact]
    public void Classic_brand_canvas_login_can_be_selected_independently_from_the_classic_studio_theme()
    {
        var services = new ServiceCollection();
        services.AddCore(options => options.Theme = StudioThemeIds.Classic);
        services
            .AddAuthenticationUI()
            .AddElsaStudioLoginThemes();
        services.Configure<LoginThemeOptions>(options => options.Theme = LoginThemeIds.ClassicBrandCanvas);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var presentation = scope.ServiceProvider.GetRequiredService<IOptions<StudioThemeOptions>>().Value;
        var studioTheme = scope.ServiceProvider.GetRequiredService<IStudioThemeRegistry>();
        var loginTheme = scope.ServiceProvider.GetRequiredService<ILoginThemeRegistry>();

        Assert.Equal(StudioThemeIds.Classic, presentation.Theme);
        Assert.Equal(StudioThemeIds.Classic, studioTheme.Selected.Id);
        Assert.Equal(LoginThemeIds.ClassicBrandCanvas, loginTheme.Selected.Id);
    }
}
