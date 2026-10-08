using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsoleLogsDashboardModule(this IServiceCollection services)
    {
        return services.AddScoped<IFeature, Feature>();
    }
}
