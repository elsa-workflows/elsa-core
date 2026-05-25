using System.Diagnostics.CodeAnalysis;
using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

internal sealed class ConsoleLogsProviderRegistration(IServiceCollection services, ServiceDescriptor defaultProviderDescriptor)
{
    private readonly object _lock = new();
    private IOptions<ConsoleLogsOptions>? _hostOptions;
    private IConsoleLogSourceRegistry? _hostSourceRegistry;

    public void ConfigureProvider(IServiceProvider serviceProvider)
    {
        var descriptor = GetCustomProviderDescriptor();
        if (descriptor == null)
            return;

        ConsoleLogsHost.ConfigureProvider((options, sourceRegistry) => ResolveProvider(serviceProvider, options, sourceRegistry));
    }

    public bool TryGetHostOptions([NotNullWhen(true)] out IOptions<ConsoleLogsOptions>? options)
    {
        lock (_lock)
        {
            options = _hostOptions;
            return options != null;
        }
    }

    public bool TryGetHostSourceRegistry([NotNullWhen(true)] out IConsoleLogSourceRegistry? sourceRegistry)
    {
        lock (_lock)
        {
            sourceRegistry = _hostSourceRegistry;
            return sourceRegistry != null;
        }
    }

    private ServiceDescriptor? GetCustomProviderDescriptor() =>
        services.LastOrDefault(x => x.ServiceType == typeof(IConsoleLogProvider) && !ReferenceEquals(x, defaultProviderDescriptor));

    private IConsoleLogProvider ResolveProvider(IServiceProvider serviceProvider, IOptions<ConsoleLogsOptions> options, IConsoleLogSourceRegistry sourceRegistry)
    {
        lock (_lock)
        {
            _hostOptions = options;
            _hostSourceRegistry = sourceRegistry;
        }

        try
        {
            return serviceProvider.GetRequiredService<IConsoleLogProvider>();
        }
        finally
        {
            lock (_lock)
            {
                _hostOptions = null;
                _hostSourceRegistry = null;
            }
        }
    }
}
