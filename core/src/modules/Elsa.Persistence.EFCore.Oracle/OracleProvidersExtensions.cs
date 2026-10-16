using System.Reflection;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Persistence.EFCore.Oracle.Configurations;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Entities;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use Oracle.
/// </summary>
[PublicAPI]
public static class OracleProvidersExtensions
{
    private static Assembly Assembly => typeof(OracleProvidersExtensions).Assembly;

    public static TFeature UseOracle<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature,
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<OracleDbContextOptionsBuilder>? configure = null)
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseOracle(Assembly, connectionString, options, configure);
    }

    public static TFeature UseOracle<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<OracleDbContextOptionsBuilder>? configure = null
    )
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseOracle(Assembly, connectionStringFunc, options, configure);
    }

    public static TFeature UseOracle<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature,
        Assembly migrationsAssembly,
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<OracleDbContextOptionsBuilder>? configure = null
    )
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseOracle(migrationsAssembly, _ => connectionString, options, configure);
    }

    public static TFeature UseOracle<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature,
        Assembly migrationsAssembly,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<OracleDbContextOptionsBuilder>? configure = null
    )
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        options = options.ConfigureOracle();
        feature.DbContextOptionsBuilder = (sp, db) => db.UseElsaOracle(migrationsAssembly, connectionStringFunc(sp), options, configure: configure);
        return (TFeature)feature;
    }
    
    public static ElsaDbContextOptions ConfigureOracle(this ElsaDbContextOptions? options)
    {
        options ??= new();
        
        var management = new Management();
        var runtime = new Runtime();
        
        options.ConfigureModel<ManagementElsaDbContext>(modelBuilder => modelBuilder
            .ApplyConfiguration<WorkflowDefinition>(management)
            .ApplyConfiguration<WorkflowInstance>(management));
        
        options.ConfigureModel<RuntimeElsaDbContext>(modelBuilder => modelBuilder
            .ApplyConfiguration<StoredTrigger>(runtime)
            .ApplyConfiguration<WorkflowExecutionLogRecord>(runtime)
            .ApplyConfiguration<ActivityExecutionRecord>(runtime)
            .ApplyConfiguration<StoredBookmark>(runtime)
            .ApplyConfiguration<WorkflowInboxMessage>(runtime));
        
        return options;
    }

    /// <summary>
    /// Applies <see cref="ConfigureOracle"/> plus the V3_6 <c>ActivityNodeId</c> NCLOB snapshot pin.
    /// Use this from design-time factories and model-differ tests only. Pinning
    /// <c>ActivityNodeId</c> at runtime changes <c>==</c> filters to
    /// <c>DBMS_LOB.COMPARE</c> and <c>Contains</c> binds to <c>TO_NCLOB</c>.
    /// </summary>
    public static ElsaDbContextOptions ConfigureOracleDesignTime(this ElsaDbContextOptions? options)
    {
        options = options.ConfigureOracle();

        options.ConfigureModel<RuntimeElsaDbContext>(modelBuilder =>
        {
            modelBuilder.Entity<ActivityExecutionRecord>().Property(x => x.ActivityNodeId).HasColumnType("NCLOB");
            modelBuilder.Entity<WorkflowExecutionLogRecord>().Property(x => x.ActivityNodeId).HasColumnType("NCLOB");
        });

        return options;
    }

    public static ElsaDbContextOptions Configure(this ElsaDbContextOptions options) => options.ConfigureOracle();
}
