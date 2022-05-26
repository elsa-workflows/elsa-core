using Elsa.Expressions.Services;
using Elsa.ServiceConfiguration.Abstractions;
using Elsa.ServiceConfiguration.Attributes;
using Elsa.ServiceConfiguration.Services;
using Elsa.Workflows.Core.Configuration;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Implementations;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Management.Providers;
using Elsa.Workflows.Management.Serialization;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Persistence.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Configuration;

[Dependency(typeof(WorkflowsConfigurator))]
[Dependency(typeof(WorkflowPersistenceConfigurator))]
public class WorkflowManagementConfigurator : ConfiguratorBase
{
    public WorkflowManagementConfigurator(IServiceConfiguration serviceConfiguration) : base(serviceConfiguration)
    {
    }
    
    public HashSet<Type> ActivityTypes { get; } = new();

    public WorkflowManagementConfigurator AddActivity<T>() where T : IActivity
    {
        ActivityTypes.Add(typeof(T));
        return this;
    }

    public override void Apply()
    {
        Services
            .AddSingleton<IWorkflowPublisher, WorkflowPublisher>()
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