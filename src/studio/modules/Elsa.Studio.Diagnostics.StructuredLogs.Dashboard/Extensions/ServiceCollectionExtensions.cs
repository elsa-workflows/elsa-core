using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Diagnostics.StructuredLogs.Dashboard.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStructuredLogsDashboardModule(this IServiceCollection services)
    {
        return services.AddScoped<IFeature, Feature>();
    }
}
