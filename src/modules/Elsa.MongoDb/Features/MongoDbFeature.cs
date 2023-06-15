using System.Security.Authentication;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;
using Elsa.MongoDb.HostedServices;
using Elsa.MongoDb.Modules.Identity;
using Elsa.MongoDb.Modules.Labels;
using Elsa.MongoDb.Modules.Management;
using Elsa.MongoDb.Modules.Runtime;
using Elsa.MongoDB.Options;
using Elsa.MongoDb.Serializers;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
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
    public string ConnectionString { get; set; }
    
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
        
        Services
            .AddSingleton<IApplicationStore, MongoApplicationStore>()
            .AddSingleton<IUserStore, MongoUserStore>()
            .AddSingleton<IRoleStore, MongoRoleStore>()
            .AddSingleton<ILabelStore, MongoLabelStore>()
            .AddSingleton<IWorkflowDefinitionLabelStore, MongoWorkflowDefinitionLabelStore>()
            .AddSingleton<IWorkflowDefinitionStore, MongoWorkflowDefinitionStore>()
            .AddSingleton<IWorkflowInstanceStore, MongoWorkflowInstanceStore>()
            .AddSingleton<IWorkflowExecutionLogStore, MongoWorkflowExecutionLogStore>()
            .AddSingleton<IWorkflowStateStore, MongoWorkflowStateStore>()
            .AddSingleton<ITriggerStore, MongoTriggerStore>()
            .AddSingleton<IBookmarkStore, MongoBookmarkStore>()
            .AddHostedService<CreateIndices>()
            .AddHealthChecks();
    }

    private void RegisterSerializers()
    {
        BsonSerializer.RegisterSerializer(typeof(object), new PolymorphicSerializer());
        BsonSerializer.RegisterSerializer(typeof(Type), new TypeSerializer());
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