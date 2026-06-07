using CShells.Features;
using Elsa.Common.Serialization;
using Elsa.Common.ShellFeatures;
using Elsa.Common;
using Elsa.Expressions.ShellFeatures;
using Elsa.Extensions;
using Elsa.Workflows.ActivationValidators;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Activities.Flowchart.Options;
using Elsa.Workflows.Activities.Flowchart.Serialization;
using Elsa.Workflows.Builders;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.LogPersistence.Strategies;
using Elsa.Workflows.LogPersistence;
using Elsa.Workflows.Middleware.Activities;
using Elsa.Workflows.Middleware.Workflows;
using Elsa.Workflows.Options;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.PortResolvers;
using Elsa.Workflows.Serialization.Configurators;
using Elsa.Workflows.Serialization.Helpers;
using Elsa.Workflows.Serialization.Serializers;
using Elsa.Workflows.Services;
using Elsa.Workflows.State;
using Elsa.Workflows.UIHints.CheckList;
using Elsa.Workflows.UIHints.Dictionary;
using Elsa.Workflows.UIHints.Dropdown;
using Elsa.Workflows.UIHints.JsonEditor;
using Elsa.Workflows.UIHints.RadioList;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Elsa.Workflows.ShellFeatures;

[ManifestFeatureCategory(ManifestFeatureCategories.Workflows)]
[ShellFeature(
    DisplayName = "Workflows",
    Description = "Provides core workflow execution, activity execution, and workflow serialization services",
    DependsOn =
[
    typeof(SystemClockFeature),
    typeof(ExpressionsFeature),
    typeof(MediatorFeature),
    typeof(DefaultFormattersFeature),
    typeof(MultitenancyFeature),
    typeof(CommitStrategiesFeature)
])]
public class WorkflowsFeature : IShellFeature
{
    /// <summary>
    /// A delegate to configure the <see cref="IWorkflowExecutionPipeline"/>.
    /// </summary>
    public Action<IWorkflowExecutionPipelineBuilder> WorkflowExecutionPipeline { get; set; } = builder => builder
        .UseExceptionHandling()
        .UseDefaultActivityScheduler();

    /// <summary>
    /// A delegate to configure the <see cref="IActivityExecutionPipeline"/>.
    /// </summary>
    public Action<IActivityExecutionPipelineBuilder> ActivityExecutionPipeline { get; set; } = builder => builder
        .UseLogging()
        .UseExceptionHandling()
        .UseExecutionLogging()
        .UseNotifications()
        .UseDefaultActivityInvoker();

    /// <summary>
    /// A factory that instantiates a concrete <see cref="IStandardInStreamProvider"/>.
    /// </summary>
    public Func<IServiceProvider, IStandardInStreamProvider> StandardInStreamProvider { get; set; } = _ => new StandardInStreamProvider(Console.In);

    /// <summary>
    /// A factory that instantiates a concrete <see cref="IStandardOutStreamProvider"/>.
    /// </summary>
    public Func<IServiceProvider, IStandardOutStreamProvider> StandardOutStreamProvider { get; set; } = _ => new StandardOutStreamProvider(Console.Out);

    /// <summary>
    /// A factory that instantiates a concrete <see cref="ILoggerStateGenerator{WorkflowExecutionContext}"/>.
    /// </summary>
    public Func<IServiceProvider, ILoggerStateGenerator<WorkflowExecutionContext>> WorkflowLoggerStateGenerator { get; set; } = sp => new WorkflowLoggerStateGenerator();

    /// <summary>
    /// A factory that instantiates a concrete <see cref="ILoggerStateGenerator{ActivityExecutionContext}"/>.
    /// </summary>
    public Func<IServiceProvider, ILoggerStateGenerator<ActivityExecutionContext>> ActivityLoggerStateGenerator { get; set; } = sp => new ActivityLoggerStateGenerator();

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<SerializationTypeOptions>(options =>
        {
            options.AddTypeAlias<ExceptionState>(nameof(ExceptionState));
            options.AddTypeAlias<FaultException>(nameof(FaultException));
            options.AddTypeAlias<VariablesDictionary>(nameof(VariablesDictionary));
            options.AddTypeAlias<Token>(nameof(Token));
            options.RegisterLegacyTypeName(typeof(FlowJoinMode), "Elsa.Workflows.Core.Activities.Flowchart.Models.FlowJoinMode, Elsa.Workflows.Core");
            options.AddTypeAliasWithLegacyName<FlowJoinMode>(nameof(FlowJoinMode));
            options.AddTypeAliasWithLegacyName<WorkflowStorageDriver>(nameof(WorkflowStorageDriver));
            options.AddTypeAliasWithLegacyName<WorkflowInstanceStorageDriver>(nameof(WorkflowInstanceStorageDriver));
            options.AddTypeAliasWithLegacyName<MemoryStorageDriver>(nameof(MemoryStorageDriver));
            options.AddTypeAliasWithLegacyName<FaultStrategy>(nameof(FaultStrategy));
            options.AddTypeAliasWithLegacyName<ContinueWithIncidentsStrategy>(nameof(ContinueWithIncidentsStrategy));
            options.AddTypeAlias<Exception>(nameof(Exception));
            options.AddTypeAlias<ArgumentException>(nameof(ArgumentException));
            options.AddTypeAlias<ArgumentNullException>(nameof(ArgumentNullException));
            options.AddTypeAlias<InvalidOperationException>(nameof(InvalidOperationException));
            options.AddTypeAlias<NullReferenceException>(nameof(NullReferenceException));
            options.AddTypeAlias<OperationCanceledException>(nameof(OperationCanceledException));
            options.AddTypeAlias<TaskCanceledException>(nameof(TaskCanceledException));
            options.AddTypeAlias<TimeoutException>(nameof(TimeoutException));
            options.AddTypeAlias<NotSupportedException>(nameof(NotSupportedException));
            options.AddTypeAlias<JObject>(nameof(JObject));
            options.AddTypeAlias<JArray>(nameof(JArray));
        });

        services
            // Core.
            .AddScoped<IActivityInvoker, ActivityInvoker>()
            .AddScoped<IWorkflowRunner, WorkflowRunner>()
            .AddScoped<IActivityTestRunner, ActivityTestRunner>()
            .AddScoped<IActivityVisitor, ActivityVisitor>()
            .AddScoped<IIdentityGraphService, IdentityGraphService>()
            .AddScoped<IWorkflowGraphBuilder, WorkflowGraphBuilder>()
            .AddScoped<IWorkflowStateExtractor, WorkflowStateExtractor>()
            .AddScoped<IActivitySchedulerFactory, ActivitySchedulerFactory>()
            .AddSingleton<IWorkflowExecutionContextSchedulerStrategy, WorkflowExecutionContextSchedulerStrategy>()
            .AddSingleton<IActivityExecutionContextSchedulerStrategy, ActivityExecutionContextSchedulerStrategy>()
            .AddSingleton<IHasher, Hasher>()
            .AddSingleton<IStimulusHasher, StimulusHasher>()
            .AddSingleton<IIdentityGenerator, ShortGuidIdentityGenerator>()
            .AddSingleton<IBookmarkPayloadSerializer>(sp => ActivatorUtilities.CreateInstance<BookmarkPayloadSerializer>(sp))
            .AddSingleton<IActivityDescriber, ActivityDescriber>()
            .AddSingleton<IActivityRegistry, ActivityRegistry>()
            .AddScoped<IActivityRegistryLookupService, ActivityRegistryLookupService>()
            .AddSingleton<IPropertyDefaultValueResolver, PropertyDefaultValueResolver>()
            .AddSingleton<IPropertyUIHandlerResolver, PropertyUIHandlerResolver>()
            .AddSingleton<IActivityFactory, ActivityFactory>()
            .AddTransient<WorkflowBuilder>()
            .AddScoped(typeof(Func<IWorkflowBuilder>), sp => () => sp.GetRequiredService<WorkflowBuilder>())
            .AddScoped<IWorkflowBuilderFactory, WorkflowBuilderFactory>()
            .AddScoped<IVariablePersistenceManager, VariablePersistenceManager>()
            .AddScoped<IIncidentStrategyResolver, DefaultIncidentStrategyResolver>()
            .AddScoped<IActivityStateFilterManager, DefaultActivityStateFilterManager>()
            .AddScoped<IWorkflowInstanceVariableReader, DefaultWorkflowInstanceVariableReader>()
            .AddScoped<IWorkflowInstanceVariableWriter, DefaultWorkflowInstanceVariableWriter>()
            .AddScoped<DefaultActivityInputEvaluator>()

            // Incident Strategies.
            .AddTransient<IIncidentStrategy, FaultStrategy>()
            .AddTransient<IIncidentStrategy, ContinueWithIncidentsStrategy>()

            // Pipelines.
            .AddScoped<IActivityExecutionPipeline>(sp => new ActivityExecutionPipeline(sp, builder =>
            {
                builder.UseActivityExecutionPipelineContributors(sp);
                ActivityExecutionPipeline(builder);
            }))
            .AddScoped<IWorkflowExecutionPipeline>(sp => new WorkflowExecutionPipeline(sp, builder =>
            {
                builder.UseWorkflowExecutionPipelineContributors(sp);
                WorkflowExecutionPipeline(builder);
            }))

            // Built-in activity services.
            .AddScoped<IActivityResolver, PropertyBasedActivityResolver>()
            .AddScoped<IActivityResolver, SwitchActivityResolver>()
            .AddSerializationOptionsConfigurator<AdditionalConvertersConfigurator>()
            .AddSerializationOptionsConfigurator<CustomConstructorConfigurator>()
            .AddSerializationOptionsConfigurator<FlowchartSerializationOptionConfigurator>()

            // Domain event handlers.
            .AddHandlersFrom<WorkflowsFeature>()

            // Stream providers.
            .AddScoped(StandardInStreamProvider)
            .AddScoped(StandardOutStreamProvider)

            // Storage drivers.
            .AddScoped<IStorageDriverManager, StorageDriverManager>()
            .AddStorageDriver<WorkflowStorageDriver>()
            .AddStorageDriver<WorkflowInstanceStorageDriver>()
            .AddStorageDriver<MemoryStorageDriver>()

            // Serialization.
            .AddSingleton<ISerializationTypeRegistry, SerializationTypeRegistry>()
            .AddSingleton<IWorkflowStateSerializer, JsonWorkflowStateSerializer>()
            .AddSingleton<IPayloadSerializer, JsonPayloadSerializer>()
            .AddSingleton<IActivitySerializer, JsonActivitySerializer>()
            .AddSingleton<IApiSerializer, ApiSerializer>()
            .AddSingleton<ISafeSerializer, SafeSerializer>()
            .AddSingleton<IJsonSerializer, StandardJsonSerializer>()
            .AddSingleton<SyntheticPropertiesWriter>()
            .AddSingleton<ActivityWriter>()

            // Instantiation strategies.
            .AddScoped<IWorkflowActivationStrategy, AllowAlwaysStrategy>()

            // UI.
            .AddScoped<IUIHintHandler, DropDownUIHintHandler>()
            .AddScoped<IUIHintHandler, CheckListUIHintHandler>()
            .AddScoped<IUIHintHandler, RadioListUIHintHandler>()
            .AddScoped<IUIHintHandler, JsonEditorUIHintHandler>()
            .AddScoped<IPropertyUIHandler, StaticCheckListOptionsProvider>()
            .AddScoped<IPropertyUIHandler, StaticRadioListOptionsProvider>()
            .AddScoped<IPropertyUIHandler, StaticDropDownOptionsProvider>()
            .AddScoped<IPropertyUIHandler, JsonCodeOptionsProvider>()
            .AddScoped<DictionaryValueEvaluator>()
            .AddSingleton<IActivityDescriptorModifier, DictionaryUIHintInputModifier>()

            // Logger state generators.
            .AddSingleton(WorkflowLoggerStateGenerator)
            .AddSingleton(ActivityLoggerStateGenerator)

            // Log Persistence Strategies.
            .AddScoped<ILogPersistenceStrategyService, DefaultLogPersistenceStrategyService>()
            .AddScoped<ILogPersistenceStrategy, Include>()
            .AddScoped<ILogPersistenceStrategy, Exclude>()
            .AddScoped<ILogPersistenceStrategy, Inherit>()
            .AddScoped<ILogPersistenceStrategy, Configuration>()

            // Logging
            .AddLogging();
        
        // Overridable services
        services.AddScoped<ICommitStateHandler, NoopCommitStateHandler>();
    }
}
