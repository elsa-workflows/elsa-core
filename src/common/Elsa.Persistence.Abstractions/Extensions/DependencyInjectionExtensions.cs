using Elsa.Persistence.Common.Entities;
using Elsa.Persistence.Common.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Persistence.Common.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddMemoryStore<TEntity, TStore>(this IServiceCollection services) where TEntity : Entity where TStore : class
    {
         services.TryAddSingleton<MemoryStore<TEntity>>();
         services.TryAddSingleton<TStore>();
         return services;
    }
}