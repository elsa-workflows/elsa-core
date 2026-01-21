using System.Reflection;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.Oracle;

/// <summary>
/// Configures Oracle as the database provider.
/// </summary>
public class OracleProviderConfigurator : DatabaseProviderConfigurator<OracleDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OracleProviderConfigurator"/> class.
    /// </summary>
    /// <param name="migrationsAssembly">The assembly containing Oracle migrations.</param>
    public OracleProviderConfigurator(Assembly migrationsAssembly) : base(migrationsAssembly)
    {
    }

    /// <inheritdoc />
    protected override DbContextOptionsBuilder ConfigureProvider(
        DbContextOptionsBuilder builder,
        Assembly assembly,
        string connectionString,
        ElsaDbContextOptions? options,
        Action<OracleDbContextOptionsBuilder>? configure)
    {
        DbContextOptions ??= new();
        DbContextOptions.Configure();
        return builder.UseElsaOracle(assembly, connectionString, DbContextOptions, configure);
    }
}
