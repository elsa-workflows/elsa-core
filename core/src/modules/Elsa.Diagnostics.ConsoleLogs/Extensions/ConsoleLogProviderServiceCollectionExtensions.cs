using ConsoleLogStreaming.Core;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Diagnostics.ConsoleLogs.Extensions;

internal static class ConsoleLogProviderServiceCollectionExtensions
{
    public static void DecorateConsoleLogProvider(this IServiceCollection services)
    {
        var descriptor = services.LastOrDefault(x => x.ServiceType == typeof(IConsoleLogProvider));
        if (descriptor == null)
            return;

        services.TryAddSingleton<ElsaConsoleLogRecentBuffer>();
        services.Remove(descriptor);
        services.Add(ServiceDescriptor.Describe(
            typeof(IConsoleLogProvider),
            sp => new ElsaConsoleLogProvider(
                CreateProvider(sp, descriptor),
                sp.GetRequiredService<IConsoleLogContextAccessor>(),
                sp.GetRequiredService<ElsaConsoleLogRecentBuffer>()),
            descriptor.Lifetime));
    }

    private static IConsoleLogProvider CreateProvider(IServiceProvider serviceProvider, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance is IConsoleLogProvider instance)
            return instance;

        if (descriptor.ImplementationFactory != null)
            return (IConsoleLogProvider)descriptor.ImplementationFactory(serviceProvider)!;

        if (descriptor.ImplementationType != null)
            return (IConsoleLogProvider)ActivatorUtilities.CreateInstance(serviceProvider, descriptor.ImplementationType);

        throw new InvalidOperationException("The console log provider registration is invalid.");
    }
}
