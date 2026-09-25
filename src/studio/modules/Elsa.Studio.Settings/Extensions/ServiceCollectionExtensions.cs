using Elsa.Studio.Contracts;
using Elsa.Studio.Settings.Contracts;
using Elsa.Studio.Settings.Menu;
using Elsa.Studio.Settings.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Settings.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSettingsModule(this IServiceCollection services)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<ISettingsSectionRegistry, SettingsSectionRegistry>()
            .AddScoped<IMenuProvider, SettingsMenu>();
    }
}
