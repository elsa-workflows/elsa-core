using Elsa.Diagnostics.ConsoleLogs.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.ConsoleLogs.Extensions;

internal static class ConsoleLogContextPipelineRegistrationExtensions
{
    public static void AddConsoleLogContextPipelines(this IServiceCollection services)
    {
        services.DecorateIfRegistered<IWorkflowExecutionPipeline, ConsoleLogWorkflowExecutionPipeline>();
        services.DecorateIfRegistered<IActivityExecutionPipeline, ConsoleLogActivityExecutionPipeline>();
    }

    private static void DecorateIfRegistered<TService, TDecorator>(this IServiceCollection services)
        where TService : class
        where TDecorator : class, TService
    {
        var descriptor = services.LastOrDefault(x => x.ServiceType == typeof(TService));

        if (descriptor == null)
            return;

        var index = services.IndexOf(descriptor);
        services[index] = ServiceDescriptor.Describe(
            typeof(TService),
            sp =>
            {
                var inner = CreateInstance<TService>(sp, descriptor);
                return ActivatorUtilities.CreateInstance<TDecorator>(sp, inner);
            },
            descriptor.Lifetime);
    }

    private static TService CreateInstance<TService>(IServiceProvider serviceProvider, ServiceDescriptor descriptor)
        where TService : class
    {
        if (descriptor.ImplementationInstance != null)
            return (TService)descriptor.ImplementationInstance;

        if (descriptor.ImplementationFactory != null)
            return (TService)descriptor.ImplementationFactory(serviceProvider)!;

        return (TService)ActivatorUtilities.CreateInstance(serviceProvider, descriptor.ImplementationType!);
    }
}
