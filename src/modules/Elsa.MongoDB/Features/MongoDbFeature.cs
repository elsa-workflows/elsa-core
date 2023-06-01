using System.Security.Authentication;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Http.Models;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;
using Elsa.MongoDB.HostedServices;
using Elsa.MongoDB.Modules.Identity;
using Elsa.MongoDB.Modules.Labels;
using Elsa.MongoDB.Modules.Management;
using Elsa.MongoDB.Modules.Runtime;
using Elsa.MongoDB.Options;
using Elsa.MongoDB.Serializers;
using Elsa.MongoDB.Stores.Management;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;
using StoredBookmark = Elsa.Workflows.Runtime.Entities.StoredBookmark;
using WorkflowExecutionLogRecord = Elsa.Workflows.Runtime.Entities.WorkflowExecutionLogRecord;

namespace Elsa.MongoDB.Features;

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
    /// A delegate that configures MongoDb.
    /// </summary>
    public Action<MongoDbOptions> Options { get; set; } = _ => { };

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(Options);

        Services.AddSingleton(CreateDatabase);
        
        RegisterClassMaps();

        AddMongoCollection<Application>(Services, "applications");
        AddMongoCollection<User>(Services, "users");
        AddMongoCollection<Role>(Services, "roles");
        AddMongoCollection<Label>(Services, "labels");
        AddMongoCollection<WorkflowDefinitionLabel>(Services, "workflow-definition-labels");
        AddMongoCollection<WorkflowDefinition>(Services, "workflow-definitions");
        AddMongoCollection<WorkflowInstance>(Services, "workflow-instances");
        AddMongoCollection<WorkflowState>(Services, "workflow-definitions");
        AddMongoCollection<WorkflowExecutionLogRecord>(Services, "workflow-instances");
        AddMongoCollection<StoredTrigger>(Services, "triggers");
        AddMongoCollection<StoredBookmark>(Services, "bookmarks");

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

    private void RegisterClassMaps()
    {
        BsonClassMap.RegisterClassMap<HttpEndpointBookmarkPayload>(cm =>
        {
            cm.AutoMap();
            cm.SetDiscriminator("HttpEndpointBookmarkPayload");
        });
        
        BsonClassMap.RegisterClassMap<StoredTrigger>(cm => {
            cm.AutoMap();
            cm.GetMemberMap(c => c.Payload).SetSerializer(new PolymorphicSerializer());
        });
    }

    private static IMongoDatabase CreateDatabase(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<MongoDbOptions>>().Value;
        var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);

        settings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
        settings.ApplicationName = "elsa-workflows";
        settings.WriteConcern = WriteConcern.WMajority;
        settings.ReadConcern = ReadConcern.Available;
        settings.ReadPreference = ReadPreference.Nearest;
        settings.RetryReads = true;
        settings.RetryWrites = true;
        settings.SslSettings = new SslSettings
        {
            EnabledSslProtocols = SslProtocols.Tls12
        };

        var mongoClient = new MongoClient(settings);
        return mongoClient.GetDatabase(options.DatabaseName);
    }
    
    private static void AddMongoCollection<T>(IServiceCollection services, string collectionName)
    {
        services.AddSingleton(
            sp => sp.GetRequiredService<IMongoDatabase>()
                .GetCollection<T>(collectionName));
    }
}