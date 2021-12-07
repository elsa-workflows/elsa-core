using Elsa.WorkflowTesting.Handlers;
using Elsa.WorkflowTesting.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowTesting.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowTestingServices(this IServiceCollection services)
        {
            services
                .AddScoped<IWorkflowTestLaunchpad, WorkflowTestLaunchpad>()
                .AddScoped<IWorkflowTestService, WorkflowTestService>()
                .AddNotificationHandlersFrom<ActivityExecutionResultExecutedHandler>()
                .AddNotificationHandlersFrom<ConfigureWorkflowContextForTestHandler>()
                .AddSignalR();

            return services;
        }
    }
}