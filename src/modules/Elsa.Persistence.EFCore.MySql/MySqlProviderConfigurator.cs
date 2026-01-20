using System.Reflection;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.MySql;

/// <summary>
/// Configures MySQL as the database provider.
/// </summary>
public class MySqlProviderConfigurator : DatabaseProviderConfigurator<MySqlDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlProviderConfigurator"/> class.
    /// </summary>
    /// <param name="migrationsAssembly">The assembly containing MySQL migrations.</param>
    public MySqlProviderConfigurator(Assembly migrationsAssembly) : base(migrationsAssembly)
    {
        ConnectionString = _ => throw new InvalidOperationException("Connection string is required for MySQL.");
    }

    /// <inheritdoc />
    protected override DbContextOptionsBuilder ConfigureProvider(
        DbContextOptionsBuilder builder,
        Assembly assembly,
        string connectionString,
        ElsaDbContextOptions? options,
        Action<MySqlDbContextOptionsBuilder>? configure)
    {
        return builder.UseElsaMySql(assembly, connectionString, options, configure: configure);
    }
}
