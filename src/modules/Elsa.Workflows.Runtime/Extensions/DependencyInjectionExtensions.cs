using Elsa.Features.Services;
using Elsa.Workflows.Runtime.Features;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Extensions;

public static class DependencyInjectionExtensions
{
    public static IModule UseRuntime(this IModule module, Action<WorkflowRuntimeFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
    
    public static IServiceCollection AddWorkflowDefinitionProvider<T>(this IServiceCollection services) where T : class, IWorkflowDefinitionProvider => services.AddSingleton<IWorkflowDefinitionProvider, T>();
    public static IServiceCollection AddStimulusHandler<T>(this IServiceCollection services) where T : class, IStimulusHandler => services.AddSingleton<IStimulusHandler, T>();
    public static IServiceCollection AddInstructionInterpreter<T>(this IServiceCollection services) where T : class, IWorkflowInstructionInterpreter => services.AddSingleton<IWorkflowInstructionInterpreter, T>();
}