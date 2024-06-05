using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.MongoDb.Options;
using Microsoft.Extensions.DependencyInjection;
using static Elsa.MongoDb.Helpers.MongoDbFeatureHelper;

namespace Elsa.MongoDb.Features;

/// <summary>
/// Configures MongoDb.
/// </summary>
public class MongoDbFeature : FeatureBase
{
    /// <inheritdoc />
    public MongoDbFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// The MongoDB connection string.
    /// </summary>
    public string ConnectionString { get; set; } = default!;

    /// <summary>
    /// A delegate that configures MongoDb.
    /// </summary>
    public Action<MongoDbOptions> Options { get; set; } = _ => { };

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(Options);
        Services.AddScoped(sp => CreateDatabase(sp, ConnectionString));

        RegisterSerializers();
        RegisterClassMaps();
    }
}