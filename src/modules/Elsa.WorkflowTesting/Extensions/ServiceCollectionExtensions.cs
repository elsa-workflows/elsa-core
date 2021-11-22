using Elsa.WorkflowTesting.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowTesting.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowTestingServices(this IServiceCollection services)
        {
            services.AddScoped<IWorkflowTestLaunchpad, WorkflowTestLaunchpad>();

            return services;
        }
    }
}