using Elsa.Dapper.Records;
using Elsa.Dapper.Services;
using Elsa.Framework.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dapper.Extensions;

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
        services.AddScoped<TStore>();
        services.AddScoped(sp => ActivatorUtilities.CreateInstance<Store<TRecord>>(sp, tableName, primaryKey));
        return services;
    }
}