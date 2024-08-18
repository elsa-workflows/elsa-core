using System.Reflection;
using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Common.Contracts;
using Elsa.EntityFrameworkCore.Common.EntityHandlers;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use Sqlite.
/// </summary>
public static class SqliteProvidersExtensions
{
    private static Assembly Assembly => typeof(SqliteProvidersExtensions).Assembly;
    
    /// <summary>
    /// Configures the feature to use Sqlite.
    /// </summary>
    public static TFeature UseSqlite<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, string? connectionString = null, ElsaDbContextOptions? options = null) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        connectionString ??= "Data Source=elsa.sqlite.db;Cache=Shared;";
        feature.Module.Services.AddScoped<IEntityModelCreatingHandler, SetupForSqlite>();
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(Assembly, connectionString, options);
        return (TFeature)feature;
    }
}