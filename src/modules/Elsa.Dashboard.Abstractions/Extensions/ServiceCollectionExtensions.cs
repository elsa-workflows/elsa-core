using Elsa.Dashboard.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dashboard.Abstractions.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDashboardContributor<TContributor>(this IServiceCollection services)
        where TContributor : class, IDashboardContributor
    {
        return services.AddScoped<IDashboardContributor, TContributor>();
    }
}
