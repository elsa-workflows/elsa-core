using Elsa.Persistence.Common.Entities;
using Elsa.Persistence.Common.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMemoryStore<TEntity, TStore>(this IServiceCollection services) where TEntity : Entity where TStore : class =>
        services
            .AddSingleton<MemoryStore<TEntity>>()
            .AddSingleton<TStore>();
}