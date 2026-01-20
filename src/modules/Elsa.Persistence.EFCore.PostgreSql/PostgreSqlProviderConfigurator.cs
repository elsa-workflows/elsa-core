using System.Reflection;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Elsa.Persistence.EFCore.PostgreSql;

/// <summary>
/// Configures PostgreSQL as the database provider.
/// </summary>
public class PostgreSqlProviderConfigurator : DatabaseProviderConfigurator<NpgsqlDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlProviderConfigurator"/> class.
    /// </summary>
    /// <param name="migrationsAssembly">The assembly containing PostgreSQL migrations.</param>
    public PostgreSqlProviderConfigurator(Assembly migrationsAssembly) : base(migrationsAssembly)
    {
        ConnectionString = _ => throw new InvalidOperationException("Connection string is required for PostgreSQL.");
    }

    /// <inheritdoc />
    protected override DbContextOptionsBuilder ConfigureProvider(
        DbContextOptionsBuilder builder,
        Assembly assembly,
        string connectionString,
        ElsaDbContextOptions? options,
        Action<NpgsqlDbContextOptionsBuilder>? configure)
    {
        return builder.UseElsaPostgreSql(assembly, connectionString, options, configure);
    }
}
