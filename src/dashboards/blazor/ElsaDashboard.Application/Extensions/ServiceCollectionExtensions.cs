using Blazored.Modal;
using ElsaDashboard.Application.Display;
using ElsaDashboard.Application.Services;
using ElsaDashboard.Extensions;
using ElsaDashboard.Services;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaDashboardUI(this IServiceCollection services)
        {
            return services
                .AddBlazoredModal()
                .AddScoped<IConfirmDialogService, ConfirmDialogService>()
                .AddScoped<IFlyoutPanelService, FlyoutPanelService>()
                .AddSingleton<IActivityDisplayService, ActivityDisplayService>()
                .AddActivityDisplayProvider<TimersDisplayProvider>()
                .AddActivityDisplayProvider<ConsoleDisplayProvider>();
        }
    }
}