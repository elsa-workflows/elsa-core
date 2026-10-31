using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql.Extensions;
using Elsa.Workflows.Admission.Persistence.EFCore.ShellFeatures;
using Elsa.Workflows.Admission.ShellFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql.ShellFeatures;

/// <summary>Provides selected PostgreSQL admission persistence for the isolated Shell execution host.</summary>
[ShellFeature(
    DisplayName = "PostgreSql Workflow Admission Persistence",
    Description = "Provides PostgreSql persistence for durable workflow admission",
    DependsOn = [typeof(AdmissionFeature)])]
public sealed class PostgreSqlAdmissionPersistenceShellFeature : EFCoreAdmissionPersistenceShellFeatureBase
{
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        // Shared Shell settings may also configure workflow persistence. Do not
        // change their history table while selecting this separate ledger context.
        var admissionOptions = new ElsaDbContextOptions
        {
            SchemaName = options?.SchemaName,
            MigrationsAssemblyName = options?.MigrationsAssemblyName,
            MigrationsHistoryTableName = options?.MigrationsHistoryTableName ?? "__AdmissionMigrationsHistory"
        };
        if (options != null)
        {
            admissionOptions.ProviderSpecificConfigurations = options.ProviderSpecificConfigurations;
        }
        builder.UseElsaPostgreSql(migrationsAssembly, connectionString, admissionOptions);
    }

    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddPostgreSqlEntityModelCreatingHandlers();
        services.AddPostgreSqlAdmissionStores();
        base.OnConfiguring(services);
    }
}
