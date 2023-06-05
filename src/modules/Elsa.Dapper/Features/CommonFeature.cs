using Dapper;
using Elsa.Dapper.TypeHandlers.Sqlite;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;

namespace Elsa.Dapper.Features;

/// <summary>
/// Configures common Dapper features.
/// </summary>
public class CommonFeature : FeatureBase
{
    /// <inheritdoc />
    public CommonFeature(IModule module) : base(module)
    {
        // See: https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/dapper-limitations#data-types
        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new GuidHandler());
        SqlMapper.AddTypeHandler(new TimeSpanHandler());
    }
}