using Elsa.Expressions.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Mediator.Features;
using Elsa.Workflows.Core.Features;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Implementations;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Management.Providers;
using Elsa.Workflows.Management.Serialization;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Persistence.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Features;

[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(WorkflowsFeature))]
[DependsOn(typeof(WorkflowPersistenceFeature))]
public class WorkflowManagementFeature : FeatureBase
{
    public WorkflowManagementFeature(IModule module) : base(module)
    {
    }
    
    public HashSet<Type> ActivityTypes { get; } = new();

    public WorkflowManagementFeature AddActivity<T>() where T : IActivity
    {
        ActivityTypes.Add(typeof(T));
        return this;
    }

    public override void Apply()
    {
        Services
            .AddSingleton<IWorkflowPublisher, WorkflowPublisher>()
            .AddSingleton<IWorkflowDefinitionManager, WorkflowDefinitionManager>()
            .AddSingleton<IActivityDescriber, ActivityDescriber>()
            .AddSingleton<IActivityRegistry, ActivityRegistry>()
            .AddSingleton<IActivityRegistryPopulator, ActivityRegistryPopulator>()
            .AddSingleton<IPropertyDefaultValueResolver, PropertyDefaultValueResolver>()
            .AddSingleton<IPropertyOptionsResolver, PropertyOptionsResolver>()
            .AddSingleton<IActivityProvider, TypedActivityProvider>()
            .AddSingleton<IActivityFactory, ActivityFactory>()
            .AddSingleton<IExpressionSyntaxRegistry, ExpressionSyntaxRegistry>()
            .AddSingleton<IExpressionSyntaxProvider, DefaultExpressionSyntaxProvider>()
            .AddSingleton<IExpressionSyntaxRegistryPopulator, ExpressionSyntaxRegistryPopulator>()
            .AddSingleton<ISerializationOptionsConfigurator, SerializationOptionsConfigurator>()
            .AddSingleton<IWorkflowMaterializer, ClrWorkflowMaterializer>()
            .AddSingleton<IWorkflowMaterializer, JsonWorkflowMaterializer>()
            .AddSingleton<WorkflowSerializerOptionsProvider>();

        Services.Configure<ApiOptions>(options =>
        {
            foreach (var activityType in ActivityTypes) 
                options.ActivityTypes.Add(activityType);
        });
    }
}