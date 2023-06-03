using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Common;

/// <summary>
/// Provides options for configuring Elsa's Entity Framework Core integration.
/// </summary>
public class ElsaDbContextOptionsExtension : IDbContextOptionsExtension
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElsaDbContextOptionsExtension"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    public ElsaDbContextOptionsExtension(ElsaDbContextOptions? options)
    {
        Options = options;
    }

    /// <summary>
    /// Gets the options.
    /// </summary>
    public ElsaDbContextOptions? Options { get; }

    /// <inheritdoc />
    public DbContextOptionsExtensionInfo Info => new CustomDbContextOptionsExtensionInfo(this);

    /// <inheritdoc />
    public void ApplyServices(IServiceCollection services)
    {
    }

    /// <inheritdoc />
    public void Validate(IDbContextOptions options)
    {
        if(!string.IsNullOrWhiteSpace(Options?.SchemaName) && string.IsNullOrWhiteSpace(Options.MigrationsAssemblyName))
        {
            throw new ArgumentException("MigrationsAssemblyName must be defined for manual migrations");
        }
    }
}