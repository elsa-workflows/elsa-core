using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Server.Web.Extensions;

/// <summary>
/// An options provider that ensures only a single database provider is registered at a time.
/// This prevents conflicts between SQLite and other database providers.
/// </summary>
public class DatabaseProviderOptionsProvider
{
    private readonly SqlDatabaseProvider _databaseProvider;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="databaseProvider">The database provider to use</param>
    /// <param name="serviceProvider">The service provider</param>
    public DatabaseProviderOptionsProvider(SqlDatabaseProvider databaseProvider, IServiceProvider serviceProvider)
    {
        _databaseProvider = databaseProvider;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Configures the DbContextOptionsBuilder with the specified database provider only.
    /// </summary>
    /// <param name="optionsBuilder">The DbContextOptionsBuilder</param>
    public void Configure(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureSingleDatabaseProvider(_databaseProvider, _serviceProvider);
    }
}