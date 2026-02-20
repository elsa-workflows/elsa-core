using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Alterations;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.PostgreSql.ShellFeatures.Alterations;

/// <summary>
/// Configures the alterations feature to use PostgreSql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "PostgreSql Alterations Persistence",
    Description = "Provides PostgreSql persistence for workflow alterations",
    DependsOn = ["Alterations"])]
[UsedImplicitly]
public class PostgreSqlAlterationsPersistenceShellFeature
    : EFCoreAlterationsPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaPostgreSql(migrationsAssembly, connectionString, options);
    }
}
