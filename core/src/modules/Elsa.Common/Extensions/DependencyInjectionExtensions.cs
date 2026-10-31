using Elsa.Common;
using Elsa.Common.RecurringTasks;
using Elsa.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds a memory store for the specified entity.
        /// </summary>
        public IServiceCollection AddMemoryStore<TEntity, TStore>() where TStore : class
        {
            services.TryAddSingleton<MemoryStore<TEntity>>();
            services.TryAddScoped<TStore>();
            return services;
        }

        /// <summary>
        /// Adds a serialization options configurator for the specified type.
        /// </summary>
        public IServiceCollection AddSerializationOptionsConfigurator<T>() where T : class, ISerializationOptionsConfigurator
        {
            services.AddSingleton<ISerializationOptionsConfigurator, T>();
            return services;
        }

        public IServiceCollection AddStartupTask<T>() where T : class, IStartupTask
        {
            return services.AddScoped<IStartupTask, T>();
        }

        public IServiceCollection AddBackgroundTask<T>() where T : class, IBackgroundTask
        {
            return services.AddScoped<IBackgroundTask, T>();
        }

        public IServiceCollection AddRecurringTask<T>() where T : class, IRecurringTask
        {
            return services.AddScoped<IRecurringTask, T>();
        }

        public IServiceCollection AddRecurringTask<T>(TimeSpan interval) where T : class, IRecurringTask
        {
            services.Configure<RecurringTaskOptions>(options => options.Schedule.ConfigureTask<T>(interval));
            return services.AddRecurringTask<T>();
        }
    }
}