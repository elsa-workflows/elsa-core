using Elsa.Dashboard.Abstractions.Extensions;
using Elsa.Diagnostics.StructuredLogs.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.StructuredLogs.Dashboard.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStructuredLogsDashboard(this IServiceCollection services)
    {
        return services.AddDashboardContributor<StructuredLogsDashboardContributor>();
    }
}
