using System.ComponentModel;
using System.Dynamic;
using System.Reflection;
using Elsa.Common.Features;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Features;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Management.Providers;
using Elsa.Workflows.Management.Serialization;
using Elsa.Workflows.Management.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Features;

/// <summary>
/// Installs and configures the workflow management feature.
/// </summary>
[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(SystemClockFeature))]
[DependsOn(typeof(WorkflowsFeature))]
[DependsOn(typeof(WorkflowDefinitionsFeature))]
[DependsOn(typeof(WorkflowInstancesFeature))]
[PublicAPI]
public class WorkflowManagementFeature : FeatureBase
{
    private const string PrimitivesCategory = "Primitives";
    private const string LookupsCategory = "Lookups";
    private const string DynamicCategory = "Dynamic";

    /// <inheritdoc />
    public WorkflowManagementFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A set of activity types to make available to the system. 
    /// </summary>
    public HashSet<Type> ActivityTypes { get; } = new();

    /// <summary>
    /// A set of variable types to make available to the system. 
    /// </summary>
    public HashSet<VariableDescriptor> VariableDescriptors { get; } = new()
    {
        new(typeof(object), PrimitivesCategory, "The root class for all object in the CLR System."),
        new(typeof(string), PrimitivesCategory, "Represents a static string of characters."),
        new(typeof(bool), PrimitivesCategory, "Represents a true or false value."),
        new(typeof(int), PrimitivesCategory, "A 32 bit integer."),
        new(typeof(long), PrimitivesCategory, "A 64 bit integer."),
        new(typeof(float), PrimitivesCategory, "A real number."),
        new(typeof(double), PrimitivesCategory, "A real number with double precision."),
        new(typeof(decimal), PrimitivesCategory, "A decimal number."),
        new(typeof(Guid), PrimitivesCategory, "Represents a Globally Unique Identifier."),
        new(typeof(DateTime), PrimitivesCategory, "A value type that represents a date and time."),
        new(typeof(DateTimeOffset), PrimitivesCategory, "A value type that consists of a DateTime and a time zone offset."),
        new(typeof(TimeSpan), PrimitivesCategory, "Represents a duration of time."),
        new(typeof(IDictionary<string, string>), LookupsCategory, "A dictionary with string key and values."),
        new(typeof(IDictionary<string, object>), LookupsCategory, "A dictionary with string key and object values."),
        new(typeof(ExpandoObject), DynamicCategory, "A dictionary that can be typed as dynamic to access members using dot notation.")
    };

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
    public WorkflowManagementFeature AddVariableType(Type type, string category) => AddVariableTypes(new[] { type }, category);

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

    /// <inheritdoc />
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
            .AddSingleton<IWorkflowDefinitionService, WorkflowDefinitionService>()
            .AddSingleton<IWorkflowValidator, WorkflowValidator>()
            .AddSingleton<IWorkflowDefinitionPublisher, WorkflowDefinitionPublisher>()
            .AddSingleton<IWorkflowDefinitionImporter, WorkflowDefinitionImporter>()
            .AddSingleton<IWorkflowDefinitionManager, WorkflowDefinitionManager>()
            .AddSingleton<IWorkflowInstanceManager, WorkflowInstanceManager>()
            .AddSingleton<IActivityRegistryPopulator, ActivityRegistryPopulator>()
            .AddSingleton<IExpressionSyntaxRegistry, ExpressionSyntaxRegistry>()
            .AddSingleton<IExpressionSyntaxProvider, DefaultExpressionSyntaxProvider>()
            .AddSingleton<IExpressionSyntaxRegistryPopulator, ExpressionSyntaxRegistryPopulator>()
            .AddSingleton<ISerializationOptionsConfigurator, SerializationOptionsConfigurator>()
            .AddSingleton<IWorkflowMaterializer, ClrWorkflowMaterializer>()
            .AddSingleton<IWorkflowMaterializer, JsonWorkflowMaterializer>()
            .AddSingleton<IActivityPortResolver, WorkflowDefinitionActivityPortResolver>()
            .AddActivityProvider<WorkflowDefinitionActivityProvider>()
            .AddSingleton<WorkflowDefinitionMapper>()
            .AddSingleton<VariableDefinitionMapper>()
            ;

        Services.AddNotificationHandlersFrom(GetType());

        Services.Configure<ManagementOptions>(options =>
        {
            foreach (var activityType in ActivityTypes)
                options.ActivityTypes.Add(activityType);

            foreach (var descriptor in VariableDescriptors) options.VariableDescriptors.Add(descriptor);
        });
    }
}