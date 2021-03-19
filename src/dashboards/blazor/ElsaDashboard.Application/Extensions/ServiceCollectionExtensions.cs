using System;
using Blazored.Modal;
using ElsaDashboard.Application.Display;
using ElsaDashboard.Application.Options;
using ElsaDashboard.Application.Services;
using ElsaDashboard.Extensions;
using ElsaDashboard.Services;
using MediatR;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaDashboardUI(this IServiceCollection services, Action<ElsaDashboardOptions>? configure = default)
        {
            if (configure != null)
                services.Configure(configure);
            
            return services
                .AddMediatR(typeof(FlyoutPanelService))
                .AddBlazoredModal()
                .AddScoped<IConfirmDialogService, ConfirmDialogService>()
                .AddScoped<IFlyoutPanelService, FlyoutPanelService>()
                .AddSingleton<IActivityDisplayService, ActivityDisplayService>()
                .AddActivityDisplayProvider<TimersDisplayProvider>()
                .AddActivityDisplayProvider<ConsoleDisplayProvider>();
        }
    }
}