using Blazored.Modal;
using ElsaDashboard.Application.Services;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaDashboardUI(this IServiceCollection services)
        {
            return services
                .AddBlazoredModal()
                .AddScoped<IFlyoutPanelService, FlyoutPanelService>();
        }
    }
}