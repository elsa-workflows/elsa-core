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
using Elsa.MongoDb.Serializers;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;
using StoredBookmark = Elsa.Workflows.Runtime.Entities.StoredBookmark;
using WorkflowExecutionLogRecord = Elsa.Workflows.Runtime.Entities.WorkflowExecutionLogRecord;

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

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton(_ => CreateDatabase(ConnectionString));
        
        RegisterSerializers();

        AddMongoCollection<Application>(Services, "applications");
        AddMongoCollection<User>(Services, "users");
        AddMongoCollection<Role>(Services, "roles");
        AddMongoCollection<Label>(Services, "labels");
        AddMongoCollection<WorkflowDefinitionLabel>(Services, "workflow_definition_labels");
        AddMongoCollection<WorkflowDefinition>(Services, "workflow_definitions");
        AddMongoCollection<WorkflowInstance>(Services, "workflow_instances");
        AddMongoCollection<WorkflowState>(Services, "workflow_states");
        AddMongoCollection<WorkflowExecutionLogRecord>(Services, "workflow_execution_logs");
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

    private void RegisterSerializers()
    {
        BsonSerializer.RegisterSerializer(typeof(object), new PolymorphicSerializer());
    }

    private static IMongoDatabase CreateDatabase(string connectionString)
    {
        var mongoUrl = new MongoUrl(connectionString);
        var settings = MongoClientSettings.FromUrl(mongoUrl);
        
        settings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
        settings.ApplicationName = "elsa_workflows";
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
        return mongoClient.GetDatabase(mongoUrl.DatabaseName);
    }
    
    private static void AddMongoCollection<T>(IServiceCollection services, string collectionName)
    {
        services.AddSingleton(
            sp => sp.GetRequiredService<IMongoDatabase>()
                .GetCollection<T>(collectionName));
    }
}