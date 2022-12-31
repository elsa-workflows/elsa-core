using Elsa.Common.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddMemoryStore<TEntity, TStore>(this IServiceCollection services) where TStore : class
    {
         services.TryAddSingleton<MemoryStore<TEntity>>();
         services.TryAddSingleton<TStore>();
         return services;
    }
}