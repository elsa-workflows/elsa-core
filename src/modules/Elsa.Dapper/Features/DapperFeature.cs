using Dapper;
using Elsa.Dapper.Contracts;
using Elsa.Dapper.Services;
using Elsa.Dapper.TypeHandlers.Sqlite;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dapper.Features;

/// <summary>
/// Configures common Dapper features.
/// </summary>
public class DapperFeature : FeatureBase
{
    /// <inheritdoc />
    public DapperFeature(IModule module) : base(module)
    {
        // See: https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/dapper-limitations#data-types
        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new GuidHandler());
        SqlMapper.AddTypeHandler(new TimeSpanHandler());
    }
    
    /// <summary>
    /// Gets or sets a factory that provides an <see cref="IDbConnectionProvider"/> instance.
    /// </summary>
    public Func<IServiceProvider, IDbConnectionProvider> DbConnectionProvider { get; set; } = _ => new SqliteDbConnectionProvider();

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton(DbConnectionProvider);
    }
}