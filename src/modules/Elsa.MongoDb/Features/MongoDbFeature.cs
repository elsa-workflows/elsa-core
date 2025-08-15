using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.KeyValues.Entities;
using Elsa.MongoDb.Contracts;
using Elsa.MongoDb.NamingStrategies;
using Elsa.MongoDb.Options;
using Elsa.MongoDb.Serializers;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Elsa.MongoDb.Features;

/// <summary>
/// Configures MongoDb.
/// </summary>
public class MongoDbFeature(IModule module) : FeatureBase(module)
{
    /// <summary>
    /// The MongoDB connection string.
    /// </summary>
    public string ConnectionString { get; set; } = default!;

    /// <summary>
    /// A delegate that configures MongoDb.
    /// </summary>
    public Action<MongoDbOptions> Options { get; set; } = _ => { };

    /// <summary>
    /// A delegate that creates an instance of an implementation of <see cref="ICollectionNamingStrategy"/>.
    /// </summary>
    public Func<IServiceProvider, ICollectionNamingStrategy> CollectionNamingStrategy { get; set; } = sp => sp.GetRequiredService<DefaultNamingStrategy>();

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(Options);

        var mongoUrl = new MongoUrl(ConnectionString);
        Services.AddSingleton(sp => CreateMongoClient(sp, mongoUrl));
        Services.AddScoped(sp => CreateDatabase(sp, mongoUrl));
        
        Services.TryAddScoped<DefaultNamingStrategy>();
        Services.AddScoped(CollectionNamingStrategy);

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
        TryRegisterSerializerOrSkipWhenExist(typeof(JsonNode), new JsonNodeBsonConverter());
        TryRegisterSerializerOrSkipWhenExist(typeof(System.Dynamic.ExpandoObject), new ExpandoObjectSerializer());
    }

    private static void RegisterClassMaps()
    {
        BsonClassMap.TryRegisterClassMap<StoredBookmark>(cm =>
        {
            cm.AutoMap();
        });

        BsonClassMap.TryRegisterClassMap<SerializedKeyValuePair>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.MapProperty(x => x.Key); // Needed for non-setter property
        });
        
        BsonClassMap.TryRegisterClassMap<WorkflowDefinition>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
        });
        
        BsonClassMap.TryRegisterClassMap<WorkflowInstance>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
        });
        
        BsonClassMap.TryRegisterClassMap<WorkflowState>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
        });
        
        BsonClassMap.RegisterClassMap<WorkflowOptions>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
        });
        
        BsonClassMap.RegisterClassMap<ActivityExecutionContextState>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
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
    
    private static IMongoClient CreateMongoClient(IServiceProvider sp, MongoUrl mongoUrl)
    {
        var options = sp.GetRequiredService<IOptions<MongoDbOptions>>().Value;

        var settings = MongoClientSettings.FromUrl(mongoUrl);

        // TODO: Uncomment once https://github.com/jbogard/MongoDB.Driver.Core.Extensions.DiagnosticSources/pull/41 is merged and deployed.
        //settings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
        
        settings.ApplicationName = GetApplicationName(settings);
        settings.WriteConcern = options.WriteConcern;
        settings.ReadConcern = options.ReadConcern;
        settings.ReadPreference = options.ReadPreference;
        settings.RetryReads = options.RetryReads;
        settings.RetryWrites = options.RetryWrites;
        settings.SslSettings = options.SslSettings;

        return new MongoClient(settings);
    }

    private static IMongoDatabase CreateDatabase(IServiceProvider sp, MongoUrl mongoUrl)
    {
        var client = sp.GetRequiredService<IMongoClient>();
        return client.GetDatabase(mongoUrl.DatabaseName);
    }

    private static string GetApplicationName(MongoClientSettings settings)
    {
        return string.IsNullOrWhiteSpace(settings.ApplicationName) ? "elsa_workflows" : settings.ApplicationName;
    }
}