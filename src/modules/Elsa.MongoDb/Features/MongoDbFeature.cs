using System.Text.Json;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.KeyValues.Entities;
using Elsa.MongoDb.Options;
using Elsa.MongoDb.Serializers;
using Elsa.Workflows.Memory;
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

        Services.AddScoped(sp => CreateDatabase(sp, ConnectionString));

        RegisterSerializers();
        RegisterClassMaps();
    }

    private static void RegisterSerializers()
    {
        TryRegisterSerializerOrSkipWhenExist(typeof(object), new PolymorphicSerializer());
        TryRegisterSerializerOrSkipWhenExist(typeof(Type), new TypeSerializer());
        TryRegisterSerializerOrSkipWhenExist(typeof(Variable), new VariableSerializer());
        TryRegisterSerializerOrSkipWhenExist(typeof(Version), new VersionSerializer());
        TryRegisterSerializerOrSkipWhenExist(typeof(JsonElement), new JsonElementSerializer());
    }

    private static void RegisterClassMaps()
    {
        BsonClassMap.TryRegisterClassMap<StoredBookmark>(cm =>
        {
            cm.AutoMap(); // Automatically map other properties;
        });

        BsonClassMap.RegisterClassMap<SerializedKeyValuePair>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true); // Needed for missing ID property
        });
    }

    private static void TryRegisterSerializerOrSkipWhenExist(Type type, IBsonSerializer serializer)
    {
       try
       {
           BsonSerializer.TryRegisterSerializer(type, serializer);
       }
       catch (BsonSerializationException ex)
       {
       }
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