using System.Reflection;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.Sqlite;

/// <summary>
/// Configures SQLite as the database provider.
/// </summary>
public class SqliteProviderConfigurator : DatabaseProviderConfigurator<SqliteDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteProviderConfigurator"/> class.
    /// </summary>
    /// <param name="migrationsAssembly">The assembly containing SQLite migrations.</param>
    public SqliteProviderConfigurator(Assembly migrationsAssembly) : base(migrationsAssembly)
    {
    }

    /// <inheritdoc />
    protected override DbContextOptionsBuilder ConfigureProvider(
        DbContextOptionsBuilder builder,
        Assembly assembly,
        string connectionString,
        ElsaDbContextOptions? options,
        Action<SqliteDbContextOptionsBuilder>? configure)
    {
        return builder.UseElsaSqlite(assembly, connectionString, options, configure);
    }
}
