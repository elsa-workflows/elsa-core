using Elsa.Server.Api.Services;
using Elsa.WorkflowTesting.Api.Handlers;
using Elsa.WorkflowTesting.Api.Services;
using Elsa.WorkflowTesting.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowTesting.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowTestingServices(this IServiceCollection services)
        {
            return services
                .AddScoped<IWorkflowTestLaunchpad, WorkflowTestLaunchpad>()
                .AddScoped<IWorkflowTestService, WorkflowTestService>()
                .AddNotificationHandlersFrom<ActivityExecutionResultExecutedHandler>()
                .AddTransient<IServerFeatureProvider, WorkflowTestingServerFeatureProvider>();
        }
    }
}