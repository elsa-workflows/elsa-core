using Elsa.Workflows.Runtime.Services;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddWorkflowDefinitionProvider<T>(this IServiceCollection services) where T : class, IWorkflowDefinitionProvider => services.AddSingleton<IWorkflowDefinitionProvider, T>();
}