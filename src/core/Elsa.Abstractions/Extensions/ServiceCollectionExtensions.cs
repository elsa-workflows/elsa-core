using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowProvider<T>(this IServiceCollection services)
            where T : class, IWorkflowProvider =>
            services.AddTransient<IWorkflowProvider, T>();
    }
}