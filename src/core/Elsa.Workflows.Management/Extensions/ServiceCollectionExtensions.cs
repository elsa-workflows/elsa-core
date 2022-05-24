using Elsa.Options;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Workflows.Management.Implementations;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Management.Providers;
using Elsa.Workflows.Management.Serialization;
using Elsa.Workflows.Management.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddElsaManagement(this ElsaOptionsConfigurator configurator)
    {
        var services = configurator.Services;
        
        return services
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
    }
    
    public static IServiceCollection ConfigureApiOptions(this IServiceCollection services, Action<ApiOptions> configure) => services.Configure(configure);
    public static IServiceCollection AddActivity<T>(this IServiceCollection services) where T:IActivity => services.ConfigureApiOptions(options => options.ActivityTypes.Add(typeof(T)));
}