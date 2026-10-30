using Elsa.Dashboard.Abstractions.Extensions;
using Elsa.Workflows.Runtime.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Dashboard.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkflowRuntimeDashboard(this IServiceCollection services)
    {
        return services.AddDashboardContributor<WorkflowDashboardContributor>();
    }
}
