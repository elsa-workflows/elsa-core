using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.Capture;
using ConsoleLogStreaming.Core.Hosting;
using ConsoleLogStreaming.Core.Providers;
using CShells.Lifecycle;
using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Extensions;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests;

[Collection(ConsoleHostStateCollection.Name)]
public class ConsoleLogsRegistrationTests : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await ConsoleLogStreamingHost.ShutdownAsync();
        ConsoleStreamHook.Uninstall();
    }

    public async Task DisposeAsync()
    {
        await ConsoleLogStreamingHost.ShutdownAsync();
        ConsoleStreamHook.Uninstall();
    }

    [Fact]
    public async Task AddConsoleLogsServices_ResolvesHostOwnedSingletons()
    {
        var services = new ServiceCollection();
        services.AddConsoleLogsServices();

        await using var serviceProvider = services.BuildServiceProvider();

        Assert.IsType<InMemoryConsoleLogProvider>(serviceProvider.GetRequiredService<IConsoleLogProvider>());
        var contextAccessor = serviceProvider.GetRequiredService<ConsoleLogContextAccessor>();
        Assert.Same(ConsoleLogStreamingHost.SourceRegistry, serviceProvider.GetRequiredService<IConsoleLogSourceRegistry>());
        Assert.Same(ConsoleLogStreamingHost.RedactionPipeline, serviceProvider.GetRequiredService<IConsoleLogRedactionPipeline>());
        Assert.Same(contextAccessor, serviceProvider.GetRequiredService<IConsoleLogContextAccessor>());
        Assert.Same(contextAccessor, serviceProvider.GetRequiredService<IConsoleLogMetadataAccessor>());
        AssertConsoleLogPipelineContributors(serviceProvider);
        Assert.Contains(serviceProvider.GetServices<IHostedService>(), x => x.GetType() == typeof(ConsoleLogStreamingHostedService));
        Assert.Contains(serviceProvider.GetServices<IShellInitializer>(), x => x.GetType() == typeof(ConsoleLogCaptureShellInitializer));
        Assert.Contains(serviceProvider.GetServices<IDrainHandler>(), x => x.GetType() == typeof(ConsoleLogCaptureShellDrainHandler));
        Assert.Same(ConsoleLogStreamingHost.Capture, serviceProvider.GetRequiredService<IConsoleLogCapture>());

        var services2 = new ServiceCollection();
        services2.AddConsoleLogsServices();
        await using var serviceProvider2 = services2.BuildServiceProvider();

        Assert.Same(serviceProvider.GetRequiredService<IConsoleLogProvider>(), serviceProvider2.GetRequiredService<IConsoleLogProvider>());
        Assert.Same(serviceProvider.GetRequiredService<IConsoleLogSourceRegistry>(), serviceProvider2.GetRequiredService<IConsoleLogSourceRegistry>());
        Assert.Same(serviceProvider.GetRequiredService<ConsoleLogContextAccessor>(), serviceProvider2.GetRequiredService<ConsoleLogContextAccessor>());
        Assert.Same(contextAccessor, ConsoleLogStreamingHost.MetadataAccessor);
    }

    [Fact]
    public async Task AddConsoleLogsServices_ShellInitializerStartsCapture()
    {
        var services = new ServiceCollection();
        services.AddConsoleLogsServices();

        await using var serviceProvider = services.BuildServiceProvider();
        var initializer = serviceProvider.GetServices<IShellInitializer>().OfType<ConsoleLogCaptureShellInitializer>().Single();

        await initializer.InitializeAsync(CancellationToken.None);

        var line = $"console-shell-capture-{Guid.NewGuid():N}";
        Console.WriteLine(line);

        var provider = serviceProvider.GetRequiredService<IConsoleLogProvider>();

        await AssertEventuallyAsync(async () =>
        {
            var result = await provider.GetRecentAsync(new()
            {
                Query = line,
                Limit = 10
            });

            Assert.Contains(result.Items, x => x.Text.Contains(line, StringComparison.Ordinal));
        });
    }

    [Fact]
    public async Task AddConsoleLogsServices_RegistersHostOwnedDependencies()
    {
        var services = new ServiceCollection();
        services.AddConsoleLogsServices();

        await using var serviceProvider = services.BuildServiceProvider();

        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
            await hostedService.StartAsync(CancellationToken.None);

        var contextAccessor = serviceProvider.GetRequiredService<ConsoleLogContextAccessor>();
        Assert.Same(ConsoleLogStreamingHost.Provider, serviceProvider.GetRequiredService<IConsoleLogProvider>());
        Assert.Same(ConsoleLogStreamingHost.SourceRegistry, serviceProvider.GetRequiredService<IConsoleLogSourceRegistry>());
        Assert.Same(ConsoleLogStreamingHost.RedactionPipeline, serviceProvider.GetRequiredService<IConsoleLogRedactionPipeline>());
        Assert.Same(ConsoleLogStreamingHost.Formatter, serviceProvider.GetRequiredService<ConsoleLineFormatter>());
        Assert.Same(contextAccessor, serviceProvider.GetRequiredService<IConsoleLogContextAccessor>());
        Assert.Same(contextAccessor, serviceProvider.GetRequiredService<IConsoleLogMetadataAccessor>());
        AssertConsoleLogPipelineContributors(serviceProvider);

        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
            await hostedService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ShutdownAsync_ResetsReferenceLeases()
    {
        ConsoleLogStreamingHost.AddReference();
        ConsoleLogStreamingHost.EnsureInitialized();

        await ConsoleLogStreamingHost.ShutdownAsync();

        ConsoleLogStreamingHost.AddReference();
        ConsoleLogStreamingHost.EnsureInitialized();
        await ConsoleLogStreamingHost.ReleaseReferenceAsync();

        var provider = new CustomProvider();
        Assert.True(ConsoleLogStreamingHost.ConfigureProvider(_ => provider));
        Assert.Same(provider, ConsoleLogStreamingHost.Provider);
    }

    [Fact]
    public async Task AddConsoleLogsHost_RegistersHostedFlushService()
    {
        var services = new ServiceCollection();
        services.AddConsoleLogsHost();

        await using var serviceProvider = services.BuildServiceProvider();

        var contextAccessor = serviceProvider.GetRequiredService<ConsoleLogContextAccessor>();
        Assert.Contains(serviceProvider.GetServices<IHostedService>(), x => x.GetType() == typeof(ConsoleLogStreamingHostedService));
        Assert.Same(contextAccessor, serviceProvider.GetRequiredService<IConsoleLogContextAccessor>());
        Assert.Same(contextAccessor, serviceProvider.GetRequiredService<IConsoleLogMetadataAccessor>());
        AssertConsoleLogPipelineContributors(serviceProvider);
    }

    [Fact]
    public async Task AddConsoleLogsHost_CapturesConsoleOutput()
    {
        var services = new ServiceCollection();
        services.AddConsoleLogsHost();

        await using var serviceProvider = services.BuildServiceProvider();
        var hostedServices = serviceProvider.GetServices<IHostedService>().ToList();

        foreach (var hostedService in hostedServices)
            await hostedService.StartAsync(CancellationToken.None);

        try
        {
            var line = $"console-capture-{Guid.NewGuid():N}";
            Console.WriteLine(line);

            var provider = serviceProvider.GetRequiredService<IConsoleLogProvider>();

            await AssertEventuallyAsync(async () =>
            {
                var result = await provider.GetRecentAsync(new()
                {
                    Query = line,
                    Limit = 10
                });

                Assert.Contains(result.Items, x => x.Text.Contains(line, StringComparison.Ordinal));
            });
        }
        finally
        {
            foreach (var hostedService in hostedServices)
                await hostedService.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public void AddConsoleLogsHost_AppliesConfigurationWhenCalledBeforeFirstAccess()
    {
        var services = new ServiceCollection();
        services.AddConsoleLogsHost(options => options.RecentCapacity = 17);

        using var serviceProvider = services.BuildServiceProvider();

        Assert.Equal(17, serviceProvider.GetRequiredService<IOptions<ConsoleLogStreaming.Core.Options.ConsoleLogOptions>>().Value.RecentCapacity);
    }

    [Fact]
    public void AddConsoleLogsHost_DoesNotInitializeHostDuringRegistration()
    {
        var services = new ServiceCollection();
        services.AddConsoleLogsHost();

        Assert.True(ConsoleLogStreamingHost.Configure(options => options.RecentCapacity = 23));

        using var serviceProvider = services.BuildServiceProvider();

        Assert.Equal(23, serviceProvider.GetRequiredService<IOptions<ConsoleLogStreaming.Core.Options.ConsoleLogOptions>>().Value.RecentCapacity);
    }

    private static void AssertConsoleLogPipelineContributors(IServiceProvider serviceProvider)
    {
        Assert.Contains(serviceProvider.GetServices<IWorkflowExecutionPipelineContributor>(), x => x.GetType() == typeof(ConsoleLogWorkflowExecutionPipelineContributor));
        Assert.Contains(serviceProvider.GetServices<IActivityExecutionPipelineContributor>(), x => x.GetType() == typeof(ConsoleLogActivityExecutionPipelineContributor));
    }

    private static async Task AssertEventuallyAsync(Func<Task> assertion)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(3);
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                await assertion();
                return;
            }
            catch (Exception e)
            {
                lastException = e;
                await Task.Delay(25);
            }
        }

        if (lastException != null)
            throw lastException;
    }

    private sealed class CustomProvider : IConsoleLogProvider
    {
        public ValueTask PublishAsync(ConsoleLogStreaming.Core.Models.ConsoleLogLine line, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask<ConsoleLogStreaming.Core.Models.RecentConsoleLogsResult> GetRecentAsync(ConsoleLogStreaming.Core.Models.ConsoleLogFilter filter, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new ConsoleLogStreaming.Core.Models.RecentConsoleLogsResult());

        public async IAsyncEnumerable<ConsoleLogStreaming.Core.Models.ConsoleLogStreamingItem> SubscribeAsync(
            ConsoleLogStreaming.Core.Models.ConsoleLogFilter filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask<IReadOnlyCollection<ConsoleLogStreaming.Core.Models.ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyCollection<ConsoleLogStreaming.Core.Models.ConsoleLogSource>>([]);
    }
}
