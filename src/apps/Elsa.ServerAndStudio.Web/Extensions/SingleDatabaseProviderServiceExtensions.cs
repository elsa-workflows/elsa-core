using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServerAndStudio.Web.Extensions;

/// <summary>
/// Extension methods for registering a single database provider in services.
/// </summary>
public static class SingleDatabaseProviderServiceExtensions
{
    /// <summary>
    /// Registers a single database provider option for Entity Framework Core.
    /// This prevents the conflict between multiple database providers when creating custom features.
    /// </summary>
    public static IServiceCollection AddSingleDatabaseProvider(
        this IServiceCollection services,
        SqlDatabaseProvider databaseProvider)
    {
        // Register the options provider as a singleton
        services.AddSingleton(sp => new DatabaseProviderOptionsProvider(databaseProvider, sp));

        // Replace EF Core's options configuration with our custom provider
        services.AddSingleton<Action<DbContextOptionsBuilder>>(sp => 
            sp.GetRequiredService<DatabaseProviderOptionsProvider>().Configure);

        return services;
    }
}