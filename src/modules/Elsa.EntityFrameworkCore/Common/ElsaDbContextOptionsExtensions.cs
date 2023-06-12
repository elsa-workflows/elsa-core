using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.EntityFrameworkCore.Common;

/// <summary>
/// Provides options for configuring Elsa's Entity Framework Core integration.
/// </summary>
public static class ElsaDbContextOptionsExtensions
{
    /// <summary>
    /// Installs a custom extension for Elsa's Entity Framework Core integration.
    /// </summary>
    /// <param name="optionsBuilder">The options builder to install the extension on.</param>
    /// <param name="options">The options to install.</param>
    public static DbContextOptionsBuilder UseElsaDbContextOptions(this DbContextOptionsBuilder optionsBuilder, ElsaDbContextOptions? options)
    {
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(new ElsaDbContextOptionsExtension(options));
        return optionsBuilder;
    }
}
