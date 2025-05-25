using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Server.Web.Extensions;

/// <summary>
/// This is an example of how to create a custom persistence feature that uses the single database provider approach.
/// </summary>
public class ExampleCustomPersistenceFeature : FeatureBase
{
    private readonly SqlDatabaseProvider _databaseProvider;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ExampleCustomPersistenceFeature(IModule module, SqlDatabaseProvider databaseProvider) : base(module)
    {
        _databaseProvider = databaseProvider;
    }

    /// <summary>
    /// Apply the feature configuration.
    /// </summary>
    public override void Apply()
    {
        // Instead of directly registering the database provider (which could cause conflicts),
        // use the DatabaseProviderOptionsProvider which ensures only one provider is registered.
        Services.AddDbContext<CustomDbContext>((sp, options) =>
        {
            // Get the provider options from DI
            var dbProviderOptions = sp.GetRequiredService<DatabaseProviderOptionsProvider>();
            
            // Configure the DbContext with the single provider
            dbProviderOptions.Configure(options);
            
            // Add any additional configuration
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });
    }
    
    /// <summary>
    /// Example DbContext class.
    /// </summary>
    private class CustomDbContext : DbContext
    {
        public CustomDbContext(DbContextOptions<CustomDbContext> options) : base(options)
        {
        }
        
        // Add your entity configurations and DbSets here
    }
}

/// <summary>
/// Extensions to register the example custom feature.
/// </summary>
public static class ExampleCustomPersistenceFeatureExtensions
{
    /// <summary>
    /// Adds the example custom persistence feature.
    /// </summary>
    public static IModule UseExampleCustomPersistence(this IModule module, SqlDatabaseProvider databaseProvider)
    {
        module.AddFeature<ExampleCustomPersistenceFeature>(databaseProvider);
        return module;
    }
}