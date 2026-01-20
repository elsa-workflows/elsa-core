using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore;

/// <summary>
/// Base class for database provider configurations that can be applied to any persistence feature.
/// </summary>
public abstract class DatabaseProviderConfigurator<TOptionsBuilder>
{
    /// <summary>
    /// Gets the assembly containing migrations for this provider.
    /// </summary>
    protected Assembly MigrationsAssembly { get; }

    /// <summary>
    /// Gets or sets the connection string to use.
    /// </summary>
    public Func<IServiceProvider, string> ConnectionString { get; set; }
        = _ => throw new InvalidOperationException("Connection string is required.");

    /// <summary>
    /// Gets or sets additional options to configure the database context.
    /// </summary>
    public ElsaDbContextOptions? DbContextOptions { get; set; }

    /// <summary>
    /// Gets or sets a callback to configure provider-specific options.
    /// </summary>
    public Action<TOptionsBuilder>? ProviderOptions { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseProviderConfigurator{TOptionsBuilder}"/> class.
    /// </summary>
    /// <param name="migrationsAssembly">The assembly containing migrations for this provider.</param>
    protected DatabaseProviderConfigurator(Assembly migrationsAssembly)
    {
        MigrationsAssembly = migrationsAssembly;
    }

    /// <summary>
    /// Configures the database provider for the specified <see cref="DbContextOptionsBuilder"/>.
    /// </summary>
    protected abstract DbContextOptionsBuilder ConfigureProvider(
        DbContextOptionsBuilder builder,
        Assembly assembly,
        string connectionString,
        ElsaDbContextOptions? options,
        Action<TOptionsBuilder>? configure);

    /// <summary>
    /// Gets the <see cref="DbContextOptionsBuilder"/> configuration delegate.
    /// </summary>
    public Action<IServiceProvider, DbContextOptionsBuilder> GetDbContextOptionsBuilder()
    {
        return (sp, db) => ConfigureProvider(db, MigrationsAssembly, ConnectionString(sp), DbContextOptions, ProviderOptions);
    }
}
