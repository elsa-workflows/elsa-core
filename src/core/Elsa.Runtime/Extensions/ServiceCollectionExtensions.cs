using Elsa.Core.Extensions;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Runtime.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowsHost(this IServiceCollection services)
        {
            return services
                .AddWorkflowsInvoker()
                .AddSingleton<IWorkflowHost, WorkflowHost>();
        }
    }
}