using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Elsa.Diagnostics.ConsoleLogs.Extensions;

public static class HostServiceCollectionExtensions
{
    /// <summary>
    /// Registers the host-level console capture pipeline. Call this from <c>Program.cs</c> (ideally right
    /// after <c>ConsoleStreamHook.Install()</c>) so that the capture singleton, the in-memory ring buffer,
    /// the source registry, and the idle flush loop are owned by the root host rather than by any
    /// individual shell.
    ///
    /// <para>
    /// Shell-level features that surface console logs (REST endpoints, SignalR hub) still call
    /// <c>AddConsoleLogsServices</c> on their own service collection — those registrations resolve to the
    /// same host-owned singletons exposed by <see cref="ConsoleLogsHost"/>.
    /// </para>
    /// </summary>
    public static IServiceCollection AddConsoleLogsHost(this IServiceCollection services, Action<ConsoleLogsOptions>? configure = null)
    {
        ConsoleStreamHook.Install();

        if (configure != null)
            ConsoleLogsHost.Configure(configure);

        ConsoleLogsHost.EnsureInitialized();
        services.TryAddSingleton(ConsoleLogsHost.ScopeAccessor);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider>(ConsoleLogsHost.ScopeAccessor));
#pragma warning disable CS0618
        services.TryAddSingleton<IConsoleLogCapture, ConsoleLogCaptureAdapter>();
#pragma warning restore CS0618
        services.TryAddEnumerable(ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, ConsoleLogsHostedService>());
        return services;
    }
}

