using System.Text.Json;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.MongoDb.Options;
using Elsa.MongoDb.Serializers;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

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

        Services.AddSingleton(sp => CreateDatabase(sp, ConnectionString));

        RegisterSerializers();
        RegisterClassMaps();
    }

    private static void RegisterSerializers()
    {
        BsonSerializer.RegisterSerializer(typeof(object), new PolymorphicSerializer());
        BsonSerializer.RegisterSerializer(typeof(Type), new TypeSerializer());
        BsonSerializer.RegisterSerializer(typeof(Variable), new VariableSerializer());
        BsonSerializer.RegisterSerializer(typeof(Version), new VersionSerializer());
        BsonSerializer.RegisterSerializer(typeof(JsonElement), new JsonElementSerializer());
    }

    private static void RegisterClassMaps()
    {
        BsonClassMap.RegisterClassMap<StoredBookmark>(cm =>
        {
            cm.AutoMap(); // Automatically map other properties
            cm
                .MapIdProperty(b => b.BookmarkId)
                .SetSerializer(new StringSerializer(BsonType.String));
        });
    }

    private static IMongoDatabase CreateDatabase(IServiceProvider sp, string connectionString)
    {
        var options = sp.GetRequiredService<IOptions<MongoDbOptions>>().Value;

        var mongoUrl = new MongoUrl(connectionString);
        var settings = MongoClientSettings.FromUrl(mongoUrl);

        settings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
        settings.ApplicationName = GetApplicationName(settings);
        settings.WriteConcern = options.WriteConcern;
        settings.ReadConcern = options.ReadConcern;
        settings.ReadPreference = options.ReadPreference;
        settings.RetryReads = options.RetryReads;
        settings.RetryWrites = options.RetryWrites;
        settings.SslSettings = options.SslSettings;

        var mongoClient = new MongoClient(settings);
        return mongoClient.GetDatabase(mongoUrl.DatabaseName);
    }

    private static string GetApplicationName(MongoClientSettings settings) =>
        string.IsNullOrWhiteSpace(settings.ApplicationName) ? "elsa_workflows" : settings.ApplicationName;
}