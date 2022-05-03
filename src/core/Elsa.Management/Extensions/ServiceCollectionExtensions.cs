using Elsa.Management.Implementations;
using Elsa.Management.Materializers;
using Elsa.Management.Options;
using Elsa.Management.Providers;
using Elsa.Management.Serialization;
using Elsa.Management.Services;
using Elsa.Options;
using Elsa.Serialization;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Management.Extensions;

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