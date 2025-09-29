using System.Reflection;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Persistence.EFCore.Oracle.Configurations;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Entities;
using JetBrains.Annotations;
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
        options ??= new();
        options.Configure();
        feature.DbContextOptionsBuilder = (sp, db) => db.UseElsaOracle(migrationsAssembly, connectionStringFunc(sp), options, configure: configure);
        return (TFeature)feature;
    }
    
    public static ElsaDbContextOptions Configure(this ElsaDbContextOptions options)
    {
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
}