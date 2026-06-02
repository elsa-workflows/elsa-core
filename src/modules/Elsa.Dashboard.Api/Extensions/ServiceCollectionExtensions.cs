using Elsa.Dashboard.Abstractions.Contracts;
using Elsa.Dashboard.Api.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dashboard.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDashboardApiServices(this IServiceCollection services)
    {
        return services
            .AddScoped<DashboardRangeResolver>()
            .AddScoped<IDashboardProvider, DefaultDashboardProvider>();
    }
}
