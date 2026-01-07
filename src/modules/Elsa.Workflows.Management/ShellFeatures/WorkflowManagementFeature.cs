using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using CShells.Features;
using Elsa.Caching.Features;
using Elsa.Common.Features;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;
using Elsa.Features.Attributes;
using Elsa.Workflows.Features;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using Elsa.Workflows.Management.Handlers.Notifications;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Management.Providers;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Serialization.Serializers;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.ShellFeatures;

/// <summary>
/// Installs and configures the workflow management feature.
/// </summary>
[ShellFeature(DependsOn =
[
    "StringCompression",
    "Mediator",
    "MemoryCache",
    "SystemClock",
    "Workflows",
    "WorkflowDefinitions",
    "WorkflowInstances"
])]
[UsedImplicitly]
public class WorkflowManagementFeature : IShellFeature
{
    private const string PrimitivesCategory = "Primitives";
    private const string LookupsCategory = "Lookups";
    private const string DynamicCategory = "Dynamic";
    private const string DataCategory = "Data";
    private const string SystemCategory = "System";

    private Func<IServiceProvider, IWorkflowDefinitionStore> WorkflowDefinitionStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowDefinitionStore>();
    private Func<IServiceProvider, IWorkflowInstanceStore> WorkflowInstanceStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowInstanceStore>();
    private Func<IServiceProvider, IWorkflowDefinitionPublisher> WorkflowDefinitionPublisher { get; set; } = sp => ActivatorUtilities.CreateInstance<WorkflowDefinitionPublisher>(sp);
    private Func<IServiceProvider, IWorkflowReferenceQuery> WorkflowReferenceQuery { get; set; } = sp => ActivatorUtilities.CreateInstance<DefaultWorkflowReferenceQuery>(sp);

    /// <summary>
    /// A set of variable types to make available to the system. 
    /// </summary>
    public HashSet<VariableDescriptor> VariableDescriptors { get; } =
    [
        new(typeof(object), PrimitivesCategory, "The root class for all object in the CLR System."),
        new(typeof(string), PrimitivesCategory, "Represents a static string of characters."),
        new(typeof(bool), PrimitivesCategory, "Represents a true or false value."),
        new(typeof(int), PrimitivesCategory, "A 32 bit integer."),
        new(typeof(long), PrimitivesCategory, "A 64 bit integer."),
        new(typeof(float), PrimitivesCategory, "A 32 bit floating point number."),
        new(typeof(double), PrimitivesCategory, "A 64 bit floating point number."),
        new(typeof(decimal), PrimitivesCategory, "A decimal number."),
        new(typeof(Guid), PrimitivesCategory, "Represents a Globally Unique Identifier."),
        new(typeof(DateTime), PrimitivesCategory, "A value type that represents a date and time."),
        new(typeof(DateTimeOffset), PrimitivesCategory, "A value type that consists of a DateTime and a time zone offset."),
        new(typeof(TimeSpan), PrimitivesCategory, "Represents a duration of time."),
        new(typeof(IDictionary<string, string>), LookupsCategory, "A dictionary with string key and values."),
        new(typeof(IDictionary<string, object>), LookupsCategory, "A dictionary with string key and object values."),
        new(typeof(ExpandoObject), DynamicCategory, "A dictionary that can be typed as dynamic to access members using dot notation."),
        new(typeof(JsonElement), DynamicCategory, "A JSON element for reading a JSON structure."),
        new(typeof(JsonNode), DynamicCategory, "A JSON node for reading and writing a JSON structure."),
        new(typeof(JsonObject), DynamicCategory, "A JSON object for reading and writing a JSON structure."),
        new(typeof(byte[]), DataCategory, "A byte array."),
        new(typeof(Stream), DataCategory, "A stream.")
    ];


    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddMemoryStore<WorkflowDefinition, MemoryWorkflowDefinitionStore>()
            .AddMemoryStore<WorkflowInstance, MemoryWorkflowInstanceStore>()
            .AddActivityProvider<TypedActivityProvider>()
            .AddActivityProvider<WorkflowDefinitionActivityProvider>()
            .AddScoped<WorkflowDefinitionActivityDescriptorFactory>()
            .AddScoped<WorkflowDefinitionActivityProvider>()
            .AddScoped<IWorkflowDefinitionActivityRegistryUpdater, WorkflowDefinitionActivityRegistryUpdater>()
            .AddScoped<IWorkflowDefinitionService, WorkflowDefinitionService>()
            .AddScoped<IWorkflowSerializer, WorkflowSerializer>()
            .AddScoped<IWorkflowValidator, WorkflowValidator>()
            .AddScoped(WorkflowReferenceQuery)
            .AddScoped(WorkflowDefinitionPublisher)
            .AddScoped<IWorkflowDefinitionImporter, WorkflowDefinitionImporter>()
            .AddScoped<IWorkflowDefinitionManager, WorkflowDefinitionManager>()
            .AddScoped<IWorkflowInstanceManager, WorkflowInstanceManager>()
            .AddScoped<IWorkflowReferenceUpdater, WorkflowReferenceUpdater>()
            .AddScoped<IActivityRegistryPopulator, ActivityRegistryPopulator>()
            .AddSingleton<IExpressionDescriptorRegistry, ExpressionDescriptorRegistry>()
            .AddSingleton<IExpressionDescriptorProvider, DefaultExpressionDescriptorProvider>()
            .AddSerializationOptionsConfigurator<SerializationOptionsConfigurator>()
            .AddScoped<IWorkflowMaterializer, TypedWorkflowMaterializer>()
            .AddScoped<IWorkflowMaterializer, ClrWorkflowMaterializer>()
            .AddScoped<IWorkflowMaterializer, JsonWorkflowMaterializer>()
            .AddScoped<IActivityResolver, WorkflowDefinitionActivityResolver>()
            .AddScoped<IWorkflowInstanceVariableManager, WorkflowInstanceVariableManager>()
            .AddScoped<WorkflowDefinitionMapper>()
            .AddSingleton<VariableDefinitionMapper>()
            .AddSingleton<WorkflowStateMapper>()
            .AddScoped(WorkflowInstanceStore)
            .AddScoped(WorkflowDefinitionStore)
            .AddSingleton<IWorkflowDefinitionCacheManager, WorkflowDefinitionCacheManager>()
            .Decorate<IWorkflowDefinitionStore, CachingWorkflowDefinitionStore>()
            .Decorate<IWorkflowDefinitionService, CachingWorkflowDefinitionService>()
            .AddNotificationHandler<EvictWorkflowDefinitionServiceCache>();

        services
            .AddNotificationHandler<DeleteWorkflowInstances>()
            .AddNotificationHandler<RefreshActivityRegistry>()
            .AddNotificationHandler<UpdateConsumingWorkflows>()
            .AddNotificationHandler<ValidateWorkflow>()
            ;

        services.Configure<ManagementOptions>(options =>
        {
            foreach (var descriptor in VariableDescriptors.DistinctBy(x => x.Type))
                options.VariableDescriptors.Add(descriptor);
        });
    }
}