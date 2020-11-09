using ElsaDashboard.Blazor.Shared.Services;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaDashboard(this IServiceCollection services)
        {
            return services.AddScoped<IFlyoutPanelService, FlyoutPanelService>();
        }
    }
}