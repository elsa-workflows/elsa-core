using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore;

/// <summary>
/// Provides options for configuring Elsa's Entity Framework Core integration.
/// </summary>
public class ElsaDbContextOptionsExtension : IDbContextOptionsExtension
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElsaDbContextOptionsExtension"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    public ElsaDbContextOptionsExtension(ElsaDbContextOptions? options) : this(options, null)
    {
    }

    internal ElsaDbContextOptionsExtension(ElsaDbContextOptions? options, StoreKeyVerifier? verifyStoreKey)
    {
        VerifyStoreKey = verifyStoreKey;
        Options = options;
    }

    /// <summary>
    /// Gets the options.
    /// </summary>
    public ElsaDbContextOptions? Options { get; }

    internal StoreKeyVerifier? VerifyStoreKey { get; }

    /// <inheritdoc />
    public DbContextOptionsExtensionInfo Info => new CustomDbContextOptionsExtensionInfo(this);

    /// <inheritdoc />
    public void ApplyServices(IServiceCollection services)
    {
    }

    /// <inheritdoc />
    public void Validate(IDbContextOptions options)
    {
    }
}