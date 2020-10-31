using Elsa.Data;
using Elsa.Services;
using Elsa.Triggers;
using YesSql.Indexes;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddIndexProvider<T>(this IServiceCollection services) where T : class, IIndexProvider => services.AddSingleton<IIndexProvider, T>();
        public static IServiceCollection AddScopedIndexProvider<T>(this IServiceCollection services) where T : class, IIndexProvider => services.AddScoped<IScopedIndexProvider>();
        public static IServiceCollection AddDataMigration<T>(this IServiceCollection services) where T : class, IDataMigration => services.AddScoped<IDataMigration, T>();
        public static IServiceCollection AddWorkflowProvider<T>(this IServiceCollection services) where T : class, IWorkflowProvider => services.AddTransient<IWorkflowProvider, T>();
        public static IServiceCollection AddTriggerProvider<T>(this IServiceCollection services) where T : class, ITriggerProvider => services.AddTransient<ITriggerProvider, T>();
        public static IServiceCollection AddWorkflowContextProvider<T>(this IServiceCollection services) where T : class, IWorkflowContextProvider => services.AddTransient<IWorkflowContextProvider, T>();
    }
}