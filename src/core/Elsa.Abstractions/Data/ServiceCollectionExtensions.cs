using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using YesSql.Indexes;

namespace Elsa.Data
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflowProvider<T>(this IServiceCollection services)
            where T : class, IWorkflowProvider =>
            services.AddTransient<IWorkflowProvider, T>();

        public static IServiceCollection AddIndexProvider<T>(this IServiceCollection services)
            where T : class, IIndexProvider =>
            services.AddSingleton<IIndexProvider, T>();

        public static IServiceCollection AddScopedIndexProvider<T>(this IServiceCollection services)
            where T : class, IIndexProvider =>
            services.AddScoped<IScopedIndexProvider>();
    }
}