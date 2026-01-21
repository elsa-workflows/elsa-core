using System.Reflection;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.SqlServer;

/// <summary>
/// Configures SQL Server as the database provider.
/// </summary>
public class SqlServerProviderConfigurator : DatabaseProviderConfigurator<SqlServerDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerProviderConfigurator"/> class.
    /// </summary>
    /// <param name="migrationsAssembly">The assembly containing SQL Server migrations.</param>
    public SqlServerProviderConfigurator(Assembly migrationsAssembly) : base(migrationsAssembly)
    {
    }

    /// <inheritdoc />
    protected override DbContextOptionsBuilder ConfigureProvider(
        DbContextOptionsBuilder builder,
        Assembly assembly,
        string connectionString,
        ElsaDbContextOptions? options,
        Action<SqlServerDbContextOptionsBuilder>? configure)
    {
        return builder.UseElsaSqlServer(assembly, connectionString, options, configure);
    }
}
