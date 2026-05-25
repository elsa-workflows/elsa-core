using System.Diagnostics.CodeAnalysis;
using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

internal sealed class ConsoleLogsProviderRegistration(IServiceCollection services, ServiceDescriptor defaultProviderDescriptor)
{
    private readonly object _lock = new();
    private ConsoleLogsProviderContext? _hostContext;

    public void ConfigureProvider(IServiceProvider serviceProvider)
    {
        var descriptor = GetCustomProviderDescriptor();
        if (descriptor == null)
            return;

        ConsoleLogsHost.ConfigureProvider(context => ResolveProvider(serviceProvider, context));
    }

    public bool TryGetHostOptions([NotNullWhen(true)] out IOptions<ConsoleLogsOptions>? options)
    {
        lock (_lock)
        {
            options = _hostContext?.Options;
            return options != null;
        }
    }

    public bool TryGetHostSourceRegistry([NotNullWhen(true)] out IConsoleLogSourceRegistry? sourceRegistry)
    {
        lock (_lock)
        {
            sourceRegistry = _hostContext?.SourceRegistry;
            return sourceRegistry != null;
        }
    }

    public bool TryGetHostRedactor([NotNullWhen(true)] out IConsoleLogRedactor? redactor)
    {
        lock (_lock)
        {
            redactor = _hostContext?.Redactor;
            return redactor != null;
        }
    }

    public bool TryGetHostFormatter([NotNullWhen(true)] out ConsoleLineFormatter? formatter)
    {
        lock (_lock)
        {
            formatter = _hostContext?.Formatter;
            return formatter != null;
        }
    }

    public bool TryGetHostScopeAccessor([NotNullWhen(true)] out ConsoleLogScopeAccessor? scopeAccessor)
    {
        lock (_lock)
        {
            scopeAccessor = _hostContext?.ScopeAccessor;
            return scopeAccessor != null;
        }
    }

    private ServiceDescriptor? GetCustomProviderDescriptor() =>
        services.LastOrDefault(x => x.ServiceType == typeof(IConsoleLogProvider) && !ReferenceEquals(x, defaultProviderDescriptor));

    private IConsoleLogProvider ResolveProvider(IServiceProvider serviceProvider, ConsoleLogsProviderContext context)
    {
        lock (_lock)
            _hostContext = context;

        try
        {
            return serviceProvider.GetRequiredService<IConsoleLogProvider>();
        }
        finally
        {
            lock (_lock)
                _hostContext = null;
        }
    }
}
