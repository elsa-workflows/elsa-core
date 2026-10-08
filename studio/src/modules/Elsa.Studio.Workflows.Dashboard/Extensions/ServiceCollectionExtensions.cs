using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Workflows.Dashboard.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkflowsDashboardModule(this IServiceCollection services)
    {
        return services.AddScoped<IFeature, Feature>();
    }
}
