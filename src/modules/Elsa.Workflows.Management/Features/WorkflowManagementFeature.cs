using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Caching.Features;
using Elsa.Common.Features;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Features;
using Elsa.Workflows.LogPersistence;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Compression;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Handlers;
using Elsa.Workflows.Management.Handlers.Notification;
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

namespace Elsa.Workflows.Management.Features;

/// <summary>
/// Installs and configures the workflow management feature.
/// </summary>
[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(SystemClockFeature))]
[DependsOn(typeof(MemoryCacheFeature))]
[DependsOn(typeof(WorkflowsFeature))]
[DependsOn(typeof(WorkflowDefinitionsFeature))]
[DependsOn(typeof(WorkflowInstancesFeature))]
[PublicAPI]
public class WorkflowManagementFeature : FeatureBase
{
    private const string PrimitivesCategory = "Primitives";
    private const string LookupsCategory = "Lookups";
    private const string DynamicCategory = "Dynamic";
    private const string DataCategory = "Data";
    private const string SystemCategory = "System";

    private string CompressionAlgorithm { get; set; } = nameof(None);
    private LogPersistenceMode LogPersistenceMode { get; set; } = LogPersistenceMode.Include;
    private bool IsReadOnlyMode { get; set; }

    /// <inheritdoc />
    public WorkflowManagementFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A set of activity types to make available to the system. 
    /// </summary>
    public HashSet<Type> ActivityTypes { get; } = [];

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
        new(typeof(Stream), DataCategory, "A stream."),
        new(typeof(LogPersistenceMode), SystemCategory, "A LogPersistenceMode enum value.")
    ];

    /// <summary>
    /// Adds the specified activity type to the system.
    /// </summary>
    public WorkflowManagementFeature AddActivity<T>() where T : IActivity => AddActivity(typeof(T));

    /// <summary>
    /// Adds the specified activity type to the system.
    /// </summary>
    public WorkflowManagementFeature AddActivity(Type activityType)
    {
        ActivityTypes.Add(activityType);
        return this;
    }

    /// <summary>
    /// Adds all types implementing <see cref="IActivity"/> to the system.
    /// </summary>
    public WorkflowManagementFeature AddActivitiesFrom<TMarker>()
    {
        var activityTypes = typeof(TMarker).Assembly.GetExportedTypes()
            .Where(x => typeof(IActivity).IsAssignableFrom(x) && x is { IsAbstract: false, IsInterface: false, IsGenericType: false })
            .ToList();
        return AddActivities(activityTypes);
    }

    /// <summary>
    /// Adds the specified activity types to the system.
    /// </summary>
    public WorkflowManagementFeature AddActivities(IEnumerable<Type> activityTypes)
    {
        ActivityTypes.AddRange(activityTypes);
        return this;
    }

    /// <summary>
    /// Removes the specified activity type from the system.
    /// </summary>
    public WorkflowManagementFeature RemoveActivity<T>() where T : IActivity => RemoveActivity(typeof(T));

    /// <summary>
    /// Adds the specified activity type to the system.
    /// </summary>
    public WorkflowManagementFeature RemoveActivity(Type activityType)
    {
        ActivityTypes.Remove(activityType);
        return this;
    }

    /// <summary>
    /// Adds the specified variable type to the system.
    /// </summary>
    public WorkflowManagementFeature AddVariableType<T>(string category) => AddVariableType(typeof(T), category);

    /// <summary>
    /// Adds the specified variable type to the system.
    /// </summary>
    public WorkflowManagementFeature AddVariableType(Type type, string category) => AddVariableTypes([type], category);

    /// <summary>
    /// Adds the specified variable types to the system.
    /// </summary>
    public WorkflowManagementFeature AddVariableTypes(IEnumerable<Type> types, string category) =>
        AddVariableTypes(types.Select(x => new VariableDescriptor(x, category, x.GetCustomAttribute<DescriptionAttribute>()?.Description)));

    /// <summary>
    /// Adds the specified variable types to the system.
    /// </summary>
    public WorkflowManagementFeature AddVariableTypes(IEnumerable<VariableDescriptor> descriptors)
    {
        VariableDescriptors.AddRange(descriptors);
        return this;
    }

    /// <summary>
    /// Sets the compression algorithm to use for compressing workflow state.
    /// </summary>
    public WorkflowManagementFeature SetCompressionAlgorithm(string algorithm)
    {
        CompressionAlgorithm = algorithm;
        return this;
    }

    /// <summary>
    /// Set the default Log Persistence mode to use for worflow state (default is Include)
    /// </summary>
    /// <param name="logPersistenceMode">The mode persistence value</param>
    public WorkflowManagementFeature SetDefaultLogPersistenceMode(LogPersistenceMode logPersistenceMode)
    {
        LogPersistenceMode = logPersistenceMode;
        return this;
    }

    /// <summary>
    /// Enables or disables read-only mode for resources such as workflow definitions.
    /// </summary>
    /// <returns></returns>
    public WorkflowManagementFeature UseReadOnlyMode(bool enabled)
    {
        IsReadOnlyMode = enabled;
        return this;
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The assembly containing the specified marker type will be scanned for activity types.")]
    public override void Configure()
    {
        AddActivitiesFrom<WorkflowManagementFeature>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddMemoryStore<WorkflowDefinition, MemoryWorkflowDefinitionStore>()
            .AddMemoryStore<WorkflowInstance, MemoryWorkflowInstanceStore>()
            .AddActivityProvider<TypedActivityProvider>()
            .AddActivityProvider<WorkflowDefinitionActivityProvider>()
            .AddScoped<WorkflowDefinitionActivityProvider>()
            .AddScoped<IWorkflowDefinitionActivityRegistryUpdater, WorkflowDefinitionActivityRegistryUpdater>()
            .AddScoped<IWorkflowDefinitionService, WorkflowDefinitionService>()
            .AddScoped<IWorkflowSerializer, WorkflowSerializer>()
            .AddScoped<IWorkflowValidator, WorkflowValidator>()
            .AddScoped<IWorkflowDefinitionPublisher, WorkflowDefinitionPublisher>()
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
            .AddSingleton<ICompressionCodecResolver, CompressionCodecResolver>()
            .AddSingleton<ICompressionCodec, None>()
            .AddSingleton<ICompressionCodec, GZip>()
            .AddSingleton<ICompressionCodec, Zstd>()
            ;

        Services
            .AddNotificationHandler<DeleteWorkflowInstances>()
            .AddNotificationHandler<RefreshActivityRegistry>()
            .AddNotificationHandler<UpdateConsumingWorkflows>()
            .AddNotificationHandler<ValidateWorkflow>()
            ;

        Services.Configure<ManagementOptions>(options =>
        {
            foreach (var activityType in ActivityTypes.Distinct())
                options.ActivityTypes.Add(activityType);

            foreach (var descriptor in VariableDescriptors.DistinctBy(x => x.Type))
                options.VariableDescriptors.Add(descriptor);

            options.CompressionAlgorithm = CompressionAlgorithm;
            options.LogPersistenceMode = LogPersistenceMode;
            options.IsReadOnlyMode = IsReadOnlyMode;
        });
    }
}