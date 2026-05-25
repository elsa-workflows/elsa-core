using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Extensions;
using Elsa.Diagnostics.ConsoleLogs.Providers.InMemory;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    public async Task AddConsoleLogsServices_UsesCustomProviderRegisteredAfterConsoleLogsServices()
    {
        var provider = new CustomProvider();
        var services = new ServiceCollection();
        services.AddConsoleLogsServices();
        services.AddSingleton<IConsoleLogProvider>(provider);

        await using var serviceProvider = services.BuildServiceProvider();

        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
            await hostedService.StartAsync(CancellationToken.None);

        Assert.Same(provider, serviceProvider.GetRequiredService<IConsoleLogProvider>());
        Assert.Same(provider, ConsoleLogsHost.Provider);

        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
            await hostedService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task AddConsoleLogsServices_CreatesCustomProviderWithHostOwnedDependencies()
    {
        var services = new ServiceCollection();
        services.AddConsoleLogsServices();
        services.AddSingleton<IConsoleLogProvider, ProviderWithHostDependencies>();

        await using var serviceProvider = services.BuildServiceProvider();

        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
            await hostedService.StartAsync(CancellationToken.None);

        var provider = Assert.IsType<ProviderWithHostDependencies>(ConsoleLogsHost.Provider);
        Assert.Same(serviceProvider.GetRequiredService<IConsoleLogProvider>(), provider);
        Assert.Same(ConsoleLogsHost.Options, provider.Options);
        Assert.Same(ConsoleLogsHost.SourceRegistry, provider.SourceRegistry);
        Assert.Same(ConsoleLogsHost.Redactor, provider.Redactor);
        Assert.Same(ConsoleLogsHost.Formatter, provider.Formatter);
        Assert.Same(ConsoleLogsHost.ScopeAccessor, provider.ScopeAccessor);

        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
            await hostedService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShutdownAsync_ResetsReferenceLeases()
    {
        ConsoleLogsHost.AddReference();
        ConsoleLogsHost.EnsureInitialized();

        await ConsoleLogsHost.ShutdownAsync();

        ConsoleLogsHost.AddReference();
        ConsoleLogsHost.EnsureInitialized();
        await ConsoleLogsHost.ReleaseReferenceAsync();

        var provider = new CustomProvider();
        Assert.True(ConsoleLogsHost.ConfigureProvider((_, _) => provider));
        Assert.Same(provider, ConsoleLogsHost.Provider);
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

    [Fact]
    public void AddConsoleLogsHost_DoesNotInitializeHostDuringRegistration()
    {
        var services = new ServiceCollection();
        services.AddConsoleLogsHost();

        Assert.True(ConsoleLogsHost.Configure(options => options.RecentLogCapacity = 23));
        Assert.Equal(23, ConsoleLogsHost.Options.Value.RecentLogCapacity);
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

    private sealed class ProviderWithHostDependencies(
        IOptions<ConsoleLogsOptions> options,
        IConsoleLogSourceRegistry sourceRegistry,
        IConsoleLogRedactor redactor,
        ConsoleLineFormatter formatter,
        ConsoleLogScopeAccessor scopeAccessor) : IConsoleLogProvider
    {
        public IOptions<ConsoleLogsOptions> Options { get; } = options;
        public IConsoleLogSourceRegistry SourceRegistry { get; } = sourceRegistry;
        public IConsoleLogRedactor Redactor { get; } = redactor;
        public ConsoleLineFormatter Formatter { get; } = formatter;
        public ConsoleLogScopeAccessor ScopeAccessor { get; } = scopeAccessor;

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
