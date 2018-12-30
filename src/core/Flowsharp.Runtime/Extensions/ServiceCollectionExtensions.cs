using Flowsharp.Runtime.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Flowsharp.Runtime.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowsHost(this IServiceCollection services)
        {
            return services
                .AddSingleton<IWorkflowHost, WorkflowHost>();
        }
    }
}