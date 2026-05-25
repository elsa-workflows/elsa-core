using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Extensions;
using Elsa.Diagnostics.ConsoleLogs.Providers.InMemory;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests;

[Collection(ConsoleHostStateCollection.Name)]
public class ConsoleLogsRegistrationTests : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await ConsoleLogsHost.ShutdownAsync();
        ConsoleStreamHook.Uninstall();
    }

    public async Task DisposeAsync()
    {
        await ConsoleLogsHost.ShutdownAsync();
        ConsoleStreamHook.Uninstall();
    }

    [Fact]
    public async Task AddConsoleLogsServices_ResolvesHostOwnedSingletons()
    {
        var services = new ServiceCollection();
        services.AddConsoleLogsServices();

        await using var serviceProvider = services.BuildServiceProvider();

        Assert.IsType<InMemoryConsoleLogProvider>(serviceProvider.GetRequiredService<IConsoleLogProvider>());
        Assert.IsType<ConsoleLogSourceRegistry>(serviceProvider.GetRequiredService<IConsoleLogSourceRegistry>());
        Assert.IsType<ConsoleLogRedactor>(serviceProvider.GetRequiredService<IConsoleLogRedactor>());
        Assert.Same(ConsoleLogsHost.ScopeAccessor, serviceProvider.GetRequiredService<ConsoleLogScopeAccessor>());
        Assert.Contains(serviceProvider.GetServices<ILoggerProvider>(), x => ReferenceEquals(x, ConsoleLogsHost.ScopeAccessor));
        Assert.Contains(serviceProvider.GetServices<IHostedService>(), x => x.GetType() == typeof(ConsoleLogsHostedService));
#pragma warning disable CS0618
        Assert.IsType<ConsoleLogCaptureAdapter>(serviceProvider.GetRequiredService<IConsoleLogCapture>());
#pragma warning restore CS0618

        // Same instance must be visible across separate shell containers — console output is process-wide.
        var services2 = new ServiceCollection();
        services2.AddConsoleLogsServices();
        await using var serviceProvider2 = services2.BuildServiceProvider();

        Assert.Same(serviceProvider.GetRequiredService<IConsoleLogProvider>(), serviceProvider2.GetRequiredService<IConsoleLogProvider>());
        Assert.Same(serviceProvider.GetRequiredService<IConsoleLogSourceRegistry>(), serviceProvider2.GetRequiredService<IConsoleLogSourceRegistry>());
    }

    [Fact]
    public async Task AddConsoleLogsServices_UsesCustomProviderForCapturePipeline()
    {
        var provider = new CustomProvider();
        var services = new ServiceCollection();
        services.AddSingleton<IConsoleLogProvider>(provider);
        services.AddConsoleLogsServices();

        await using var serviceProvider = services.BuildServiceProvider();

        _ = serviceProvider.GetRequiredService<ConsoleLogScopeAccessor>();

        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
            await hostedService.StartAsync(CancellationToken.None);

        Assert.Same(provider, serviceProvider.GetRequiredService<IConsoleLogProvider>());
        Assert.Same(provider, ConsoleLogsHost.Provider);

        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
            await hostedService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task AddConsoleLogsHost_RegistersHostedFlushService()
    {
        var services = new ServiceCollection();
        services.AddConsoleLogsHost();

        await using var serviceProvider = services.BuildServiceProvider();

        Assert.Contains(serviceProvider.GetServices<IHostedService>(), x => x.GetType() == typeof(ConsoleLogsHostedService));
        Assert.Contains(serviceProvider.GetServices<ILoggerProvider>(), x => ReferenceEquals(x, ConsoleLogsHost.ScopeAccessor));
    }

    [Fact]
    public void AddConsoleLogsHost_AppliesConfigurationWhenCalledBeforeFirstAccess()
    {
        var services = new ServiceCollection();
        services.AddConsoleLogsHost(options => options.RecentLogCapacity = 17);

        Assert.Equal(17, ConsoleLogsHost.Options.Value.RecentLogCapacity);
    }

    private sealed class CustomProvider : IConsoleLogProvider
    {
        public ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new RecentConsoleLogsResult([]));

        public async IAsyncEnumerable<ConsoleLogStreamItem> SubscribeAsync(ConsoleLogFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyCollection<ConsoleLogSource>>([]);
    }
}
