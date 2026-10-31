using System.Reflection;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use SQL Server.
/// </summary>
public static class SqlServerProvidersExtensions
{
    /// <summary>
    /// The filter <c>V3_6.Up()</c> created on the StoredTrigger unique index. SQL Server's
    /// index convention would otherwise expand it to every nullable column
    /// (<c>[Hash] IS NOT NULL AND [TenantId] IS NOT NULL</c>).
    /// </summary>
    internal const string StoredTriggerUniqueIndexFilter = "[Hash] IS NOT NULL";

    private static Assembly Assembly => typeof(SqlServerProvidersExtensions).Assembly;
    
    public static TFeature UseSqlServer<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, 
        string connectionString, 
        ElsaDbContextOptions? options = null,
        Action<SqlServerDbContextOptionsBuilder>? configure = null) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseSqlServer(Assembly, connectionString, options, configure);
    }
    
    public static TFeature UseSqlServer<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, 
        Func<IServiceProvider, string> connectionStringFunc, 
        ElsaDbContextOptions? options = null,
        Action<SqlServerDbContextOptionsBuilder>? configure = null)
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseSqlServer(Assembly, connectionStringFunc, options, configure);
    }
    
    public static TFeature UseSqlServer<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, 
        Assembly migrationsAssembly, 
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<SqlServerDbContextOptionsBuilder>? configure = null) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseSqlServer(migrationsAssembly, _ => connectionString, options, configure);
    }
    
    public static TFeature UseSqlServer<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, 
        Assembly migrationsAssembly, 
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<SqlServerDbContextOptionsBuilder>? configure = null) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        options = options.ConfigureSqlServer();
        feature.DbContextOptionsBuilder = (sp, db) => db.UseElsaSqlServer(migrationsAssembly, connectionStringFunc(sp), options, configure);
        if (feature is EFCoreWorkflowRuntimePersistenceFeature runtimePersistenceFeature)
            runtimePersistenceFeature.UseWorkflowCommitTransaction = true;
        return (TFeature)feature;
    }

    /// <summary>
    /// Pins SQL Server-specific model configuration. Call once at registration, not from
    /// per-scope factory configuration such as <see cref="DbContextOptionsBuilderExtensions.UseElsaSqlServer"/>.
    /// </summary>
    public static ElsaDbContextOptions ConfigureSqlServer(this ElsaDbContextOptions? options)
    {
        options ??= new();

        var existing = options.GetModelConfigurations(typeof(RuntimeElsaDbContext));
        if (existing.GetInvocationList().Any(IsStoredTriggerUniqueIndexFilterPin))
        {
            return options;
        }

        options.ConfigureModel<RuntimeElsaDbContext>(PinStoredTriggerUniqueIndexFilter);
        return options;
    }

    // Pin the filter the database already has so SqlServerIndexConvention does not rewrite it.
    private static void PinStoredTriggerUniqueIndexFilter(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoredTrigger>()
            .HasIndex(x => new
            {
                x.WorkflowDefinitionId,
                x.Hash,
                x.ActivityId,
                x.TenantId
            })
            .IsUnique()
            .HasDatabaseName("IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_TenantId")
            .HasFilter(StoredTriggerUniqueIndexFilter);
    }

    private static bool IsStoredTriggerUniqueIndexFilterPin(Delegate callback) =>
        callback.Method == new Action<ModelBuilder>(PinStoredTriggerUniqueIndexFilter).Method;
}