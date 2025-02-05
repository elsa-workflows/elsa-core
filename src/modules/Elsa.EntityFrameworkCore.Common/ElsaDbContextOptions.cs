using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore;

/// <summary>
/// Provides options for configuring Elsa's Entity Framework Core integration.
/// </summary>
[PublicAPI]
public class ElsaDbContextOptions
{
    /// <summary>
    /// The schema used by Elsa.
    /// </summary>
    public string? SchemaName { get; set; }

    /// <summary>
    /// The table used to store the migrations history.
    /// </summary>
    public string? MigrationsHistoryTableName { get; set; }

    /// <summary>
    /// The assembly name containing the migrations.
    /// </summary>
    public string? MigrationsAssemblyName { get; set; }

    public IDictionary<Type, Action<ModelBuilder>> ProviderSpecificConfigurations { get; set; } = new Dictionary<Type, Action<ModelBuilder>>();

    public void ConfigureModel<TDbContext>(Action<ModelBuilder> configure) where TDbContext : DbContext
    {
        ConfigureModel(typeof(TDbContext), configure);
    }
    
    public void ConfigureModel(Type dbContextType, Action<ModelBuilder> configure)
    {
        if (!ProviderSpecificConfigurations.TryGetValue(dbContextType, out var configurations))
            ProviderSpecificConfigurations[dbContextType] = configurations = _ => { };
        
        configurations += configure;
        ProviderSpecificConfigurations[dbContextType] = configurations;
    }
    
    public Action<ModelBuilder> GetModelConfigurations(DbContext dbContext)
    {
        return GetModelConfigurations(dbContext.GetType());
    }
    
    public Action<ModelBuilder> GetModelConfigurations(Type dbContextType)
    {
        return ProviderSpecificConfigurations.TryGetValue(dbContextType, out var providerConfigurations) ? providerConfigurations : _ => { };
    }
}