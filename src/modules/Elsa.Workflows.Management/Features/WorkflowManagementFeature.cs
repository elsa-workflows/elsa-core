using System.ComponentModel;
using System.Reflection;
using Elsa.Common.Features;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Mediator.Features;
using Elsa.Workflows.Core.Features;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Implementations;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Management.Providers;
using Elsa.Workflows.Management.Serialization;
using Elsa.Workflows.Management.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Features;

/// <summary>
/// Installs & configures the workflow management feature.
/// </summary>
[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(SystemClockFeature))]
[DependsOn(typeof(WorkflowsFeature))]
public class WorkflowManagementFeature : FeatureBase
{
    private const string PrimitivesCategory = "Primitives";

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
        new(typeof(short), PrimitivesCategory, "A 16 bit integer."),
        new(typeof(int), PrimitivesCategory, "A 32 bit integer."),
        new(typeof(long), PrimitivesCategory, "A 64 bit integer."),
        new(typeof(float), PrimitivesCategory, "A real number."),
        new(typeof(double), PrimitivesCategory, "A real number with double precision."),
        new(typeof(decimal), PrimitivesCategory, "A decimal number."),
        new(typeof(Guid), PrimitivesCategory, "Represents a Globally Unique Identifier."),
        new(typeof(DateTime), PrimitivesCategory, "A value type that represents a date and time."),
        new(typeof(DateTimeOffset), PrimitivesCategory, "A value type that consists of a DateTime and a time zone offset."),
        new(typeof(TimeSpan), PrimitivesCategory, "Represents a duration of time."),
        new(typeof(DateOnly), PrimitivesCategory, "Represents dates with values ranging from January 1, 0001 Anno Domini (Common Era) through December 31, 9999 A.D. (C.E.) in the Gregorian calendar."),
        new(typeof(TimeOnly), PrimitivesCategory, "Represents a time of day, as would be read from a clock, within the range 00:00:00 to 23:59:59.9999999.")
    };

    /// <summary>
    /// The factory to create new instances of <see cref="IWorkflowDefinitionStore"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowDefinitionStore> WorkflowDefinitionStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowDefinitionStore>();
    

    /// <summary>
    /// Adds the specified activity type to the system.
    /// </summary>
    public WorkflowManagementFeature AddActivity<T>() where T : IActivity
    {
        ActivityTypes.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// Adds all types implementing <see cref="IActivity"/> to the system.
    /// </summary>
    public WorkflowManagementFeature AddActivitiesFrom<TMarker>()
    {
        var activityTypes = typeof(TMarker).Assembly.GetExportedTypes()
            .Where(x => typeof(IActivity).IsAssignableFrom(x) && !x.IsAbstract && !x.IsInterface && !x.IsGenericType)
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
    /// Adds the specified variable type to the system.
    /// </summary>
    public WorkflowManagementFeature AddVariableType<T>(string category) => AddVariableTypes(new[] { typeof(T) }, category);

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
    public override void Apply()
    {
        Services
            .AddMemoryStore<WorkflowDefinition, MemoryWorkflowDefinitionStore>()
            .AddMemoryStore<WorkflowInstance, MemoryWorkflowInstanceStore>()
            .AddActivityProvider<TypedActivityProvider>()
            .AddSingleton(WorkflowDefinitionStore)
            .AddSingleton<IWorkflowDefinitionPublisher, WorkflowDefinitionPublisher>()
            .AddSingleton<IWorkflowDefinitionManager, WorkflowDefinitionManager>()
            .AddSingleton<IActivityDescriber, ActivityDescriber>()
            .AddSingleton<IActivityRegistry, ActivityRegistry>()
            .AddSingleton<IActivityRegistryPopulator, ActivityRegistryPopulator>()
            .AddSingleton<IPropertyDefaultValueResolver, PropertyDefaultValueResolver>()
            .AddSingleton<IPropertyOptionsResolver, PropertyOptionsResolver>()
            .AddSingleton<IActivityFactory, ActivityFactory>()
            .AddSingleton<IExpressionSyntaxRegistry, ExpressionSyntaxRegistry>()
            .AddSingleton<IExpressionSyntaxProvider, DefaultExpressionSyntaxProvider>()
            .AddSingleton<IExpressionSyntaxRegistryPopulator, ExpressionSyntaxRegistryPopulator>()
            .AddSingleton<ISerializationOptionsConfigurator, SerializationOptionsConfigurator>()
            .AddSingleton<IWorkflowMaterializer, ClrWorkflowMaterializer>()
            .AddSingleton<IWorkflowMaterializer, JsonWorkflowMaterializer>()
            .AddSingleton<SerializerOptionsProvider>()
            .AddSingleton<VariableDefinitionMapper>()
            ;

        Services.Configure<ManagementOptions>(options =>
        {
            foreach (var activityType in ActivityTypes)
                options.ActivityTypes.Add(activityType);

            foreach (var descriptor in VariableDescriptors) options.VariableDescriptors.Add(descriptor);
        });
    }
}