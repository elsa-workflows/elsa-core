using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.Dapper.Records;
using Elsa.Persistence.Dapper.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Persistence.Dapper.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register Dapper stores.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a Dapper store.
    /// </summary>
    public static IServiceCollection AddDapperStore<TStore, TRecord>(this IServiceCollection services, string tableName, string primaryKey = nameof(Entity.Id)) where TStore : class where TRecord : Record
    {
        // Store<T> scopes queries by tenant. Hosts without the multitenancy feature still resolve it, against the default tenant.
        services.TryAddSingleton<ITenantAccessor, DefaultTenantAccessor>();
        services.AddScoped<TStore>();
        services.AddScoped(sp => ActivatorUtilities.CreateInstance<Store<TRecord>>(sp, tableName, primaryKey));
        return services;
    }
}