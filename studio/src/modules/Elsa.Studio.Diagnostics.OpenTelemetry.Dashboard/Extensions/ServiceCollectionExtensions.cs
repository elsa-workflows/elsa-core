using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Dashboard.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenTelemetryDashboardModule(this IServiceCollection services)
    {
        return services.AddScoped<IFeature, Feature>();
    }
}
