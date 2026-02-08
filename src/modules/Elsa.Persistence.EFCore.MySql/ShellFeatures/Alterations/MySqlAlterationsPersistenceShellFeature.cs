using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Alterations;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.MySql.ShellFeatures.Alterations;

/// <summary>
/// Configures the alterations feature to use MySql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "MySql Alterations Persistence",
    Description = "Provides MySql persistence for workflow alterations",
    DependsOn = ["Alterations"])]
[UsedImplicitly]
public class MySqlAlterationsPersistenceShellFeature
    : EFCoreAlterationsPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaMySql(migrationsAssembly, connectionString, options);
    }
}
