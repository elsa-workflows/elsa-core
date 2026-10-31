using Elsa.Dashboard.Abstractions.Extensions;
using Elsa.Diagnostics.ConsoleLogs.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.ConsoleLogs.Dashboard.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsoleLogsDashboard(this IServiceCollection services)
    {
        return services.AddDashboardContributor<ConsoleLogsDashboardContributor>();
    }
}
