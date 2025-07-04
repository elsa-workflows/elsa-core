using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.KeyValues.Entities;
using Elsa.MongoDb.Contracts;
using Elsa.MongoDb.HostedServices;
using Elsa.MongoDb.NamingStrategies;
using Elsa.MongoDb.Options;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
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
    public string ConnectionString { get; set; } = null!;

    /// <summary>
    /// A delegate that configures MongoDb.
    /// </summary>
    public Action<MongoDbOptions> Options { get; set; } = _ => { };

    /// <summary>
    /// A delegate that creates an instance of an implementation of <see cref="ICollectionNamingStrategy"/>.
    /// </summary>
    public Func<IServiceProvider, ICollectionNamingStrategy> CollectionNamingStrategy { get; set; } = sp => sp.GetRequiredService<DefaultNamingStrategy>();

    public override void ConfigureHostedServices()
    {
        Module.ConfigureHostedService<ConfigureMongoDbSerializers>(-10);
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(Options);

        var mongoUrl = new MongoUrl(ConnectionString);
        Services.AddSingleton(sp => CreateMongoClient(sp, mongoUrl));
        Services.AddScoped(sp => CreateDatabase(sp, mongoUrl));

        Services.TryAddScoped<DefaultNamingStrategy>();
        Services.AddScoped(CollectionNamingStrategy);

        RegisterClassMaps();
    }

    private static void RegisterClassMaps()
    {
        BsonClassMap.TryRegisterClassMap<StoredBookmark>(cm =>
        {
            cm.AutoMap(); // Automatically map other properties;
        });

        BsonClassMap.TryRegisterClassMap<SerializedKeyValuePair>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true); // Needed for missing ID property
            map.MapProperty(x => x.Key); // Needed for non-setter property
        });
        
        BsonClassMap.TryRegisterClassMap<FlowScope>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
        });
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