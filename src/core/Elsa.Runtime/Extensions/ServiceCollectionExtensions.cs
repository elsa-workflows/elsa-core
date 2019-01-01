using Elsa.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Esla.Runtime.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowsHost(this IServiceCollection services)
        {
            return services
                .AddScoped<IWorkflowHost, WorkflowHost>();
        }
    }
}