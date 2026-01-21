using CShells.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore;

/// <summary>
/// A generic shell feature that applies a database provider configuration to a persistence feature.
/// This feature automatically infers dependencies from the base persistence feature.
/// </summary>
public class DatabaseProviderShellFeature<TBaseFeature, TDbContext, TOptionsBuilder>
    : PersistenceShellFeatureBase<DatabaseProviderShellFeature<TBaseFeature, TDbContext, TOptionsBuilder>, TDbContext>,
      IInfersDependenciesFrom<TBaseFeature>
    where TBaseFeature : PersistenceShellFeatureBase<TBaseFeature, TDbContext>
    where TDbContext : ElsaDbContextBase
{
    private readonly DatabaseProviderConfigurator<TOptionsBuilder> _providerConfigurator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseProviderShellFeature{TBaseFeature, TDbContext, TOptionsBuilder}"/> class.
    /// </summary>
    /// <param name="providerConfigurator">The database provider configurator to use.</param>
    protected DatabaseProviderShellFeature(DatabaseProviderConfigurator<TOptionsBuilder> providerConfigurator)
    {
        _providerConfigurator = providerConfigurator;
    }

    /// <summary>
    /// Gets or sets the connection string to use.
    /// </summary>
    public string ConnectionString
    {
        get => _providerConfigurator.ConnectionString;
        set => _providerConfigurator.ConnectionString = value;
    }

    /// <summary>
    /// Gets or sets additional options to configure the database context.
    /// </summary>
    public ElsaDbContextOptions? DbContextOptions
    {
        get => _providerConfigurator.DbContextOptions;
        set => _providerConfigurator.DbContextOptions = value;
    }

    /// <summary>
    /// Gets or sets a callback to configure provider-specific options.
    /// </summary>
    public Action<TOptionsBuilder>? ProviderOptions
    {
        get => _providerConfigurator.ProviderOptions;
        set => _providerConfigurator.ProviderOptions = value;
    }

    /// <inheritdoc />
    protected override void OnConfiguring(IServiceCollection services)
    {
        DbContextOptionsBuilder = _providerConfigurator.GetDbContextOptionsBuilder();
        base.OnConfiguring(services);
    }
}
