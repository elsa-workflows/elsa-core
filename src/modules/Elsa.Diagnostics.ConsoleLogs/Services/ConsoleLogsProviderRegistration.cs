using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

internal sealed class ConsoleLogsProviderRegistration(IServiceCollection services, ServiceDescriptor defaultProviderDescriptor)
{
    public void ConfigureProvider(IServiceProvider serviceProvider)
    {
        var descriptor = GetCustomProviderDescriptor();
        if (descriptor == null)
            return;

        ConsoleLogsHost.ConfigureProvider((options, sourceRegistry) => CreateProvider(serviceProvider, descriptor, options, sourceRegistry));
    }

    private ServiceDescriptor? GetCustomProviderDescriptor() =>
        services.LastOrDefault(x => x.ServiceType == typeof(IConsoleLogProvider) && !ReferenceEquals(x, defaultProviderDescriptor));

    private static IConsoleLogProvider CreateProvider(IServiceProvider serviceProvider, ServiceDescriptor descriptor, IOptions<ConsoleLogsOptions> options, IConsoleLogSourceRegistry sourceRegistry)
    {
        var provider = new ConsoleLogsProviderServiceProvider(serviceProvider, options, sourceRegistry);

        if (descriptor.ImplementationInstance is IConsoleLogProvider instance)
            return instance;

        if (descriptor.ImplementationFactory != null)
            return (IConsoleLogProvider)descriptor.ImplementationFactory(provider);

        if (descriptor.ImplementationType != null)
            return (IConsoleLogProvider)ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType);

        throw new InvalidOperationException("The custom console log provider registration is not supported.");
    }

    private sealed class ConsoleLogsProviderServiceProvider(
        IServiceProvider inner,
        IOptions<ConsoleLogsOptions> options,
        IConsoleLogSourceRegistry sourceRegistry) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IOptions<ConsoleLogsOptions>))
                return options;

            if (serviceType == typeof(IConsoleLogSourceRegistry))
                return sourceRegistry;

            return inner.GetService(serviceType);
        }
    }
}
