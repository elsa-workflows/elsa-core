using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.Capture;
using ConsoleLogStreaming.Core.Models;
using CShells.Lifecycle;
using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Extensions;
using Elsa.Diagnostics.ConsoleLogs.RealTime;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task AddConsoleLogsServices_RegistersConsoleLogPipeline()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConsoleLogsServices();

        await using var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetRequiredService<IConsoleLogProvider>());
        Assert.NotNull(serviceProvider.GetRequiredService<IConsoleLogCapture>());
        Assert.NotNull(serviceProvider.GetRequiredService<IElsaConsoleLogHubAuthorizer>());
        Assert.NotNull(serviceProvider.GetRequiredService<ElsaConsoleLogSubscriptionManager>());
        Assert.Same(serviceProvider.GetRequiredService<ConsoleLogContextAccessor>(), serviceProvider.GetRequiredService<IConsoleLogContextAccessor>());
        Assert.Same(serviceProvider.GetRequiredService<ConsoleLogContextAccessor>(), serviceProvider.GetRequiredService<IConsoleLogMetadataAccessor>());
        Assert.Contains(serviceProvider.GetServices<IShellInitializer>(), x => x.GetType() == typeof(ConsoleLogCaptureShellInitializer));
        Assert.Contains(serviceProvider.GetServices<IDrainHandler>(), x => x.GetType() == typeof(ConsoleLogCaptureShellDrainHandler));
        AssertConsoleLogPipelineContributors(serviceProvider);
    }

    [Fact]
    public async Task AddConsoleLogsServices_ShellInitializerStartsCapture()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConsoleLogsServices();

        await using var serviceProvider = services.BuildServiceProvider();
        var initializer = serviceProvider.GetServices<IShellInitializer>().OfType<ConsoleLogCaptureShellInitializer>().Single();

        await initializer.InitializeAsync(CancellationToken.None);

        try
        {
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
        finally
        {
            var drainHandler = serviceProvider.GetServices<IDrainHandler>().OfType<ConsoleLogCaptureShellDrainHandler>().Single();
            await drainHandler.DrainAsync(new NoopDrainExtensionHandle(), CancellationToken.None);
        }
    }

    [Fact]
    public async Task AddConsoleLogsHost_RegistersHostedServicesAndCaptureDependencies()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConsoleLogsHost();

        await using var serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetRequiredService<IConsoleLogCapture>());
        Assert.NotNull(serviceProvider.GetRequiredService<IConsoleLogProvider>());
        Assert.Same(serviceProvider.GetRequiredService<ConsoleLogContextAccessor>(), serviceProvider.GetRequiredService<IConsoleLogContextAccessor>());
        Assert.Same(serviceProvider.GetRequiredService<ConsoleLogContextAccessor>(), serviceProvider.GetRequiredService<IConsoleLogMetadataAccessor>());
        AssertConsoleLogPipelineContributors(serviceProvider);
    }

    [Fact]
    public void AddConsoleLogsHost_AppliesConfiguration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConsoleLogsHost(options => options.RecentCapacity = 17);

        using var serviceProvider = services.BuildServiceProvider();

        Assert.Equal(17, serviceProvider.GetRequiredService<IOptions<ConsoleLogStreaming.Core.Options.ConsoleLogOptions>>().Value.RecentCapacity);
    }

    [Fact]
    public async Task DecoratedProvider_AttachesAmbientElsaMetadata()
    {
        await using var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddConsoleLogsServices(options => options.SourceId = "test-source")
            .BuildServiceProvider();
        var contextAccessor = serviceProvider.GetRequiredService<IConsoleLogContextAccessor>();
        var provider = serviceProvider.GetRequiredService<IConsoleLogProvider>();

        using (contextAccessor.PushWorkflowInstanceId("workflow-console"))
            await provider.PublishAsync(new ConsoleLogLine { Text = "message", Source = new ConsoleLogSource { Id = "test-source" } });

        var result = await provider.GetRecentAsync(new ConsoleLogFilter
        {
            Metadata = new Dictionary<string, string>
            {
                [ConsoleLogMetadataKeys.WorkflowInstanceId] = "workflow-console"
            }
        });

        var line = Assert.Single(result.Items);
        Assert.Equal("workflow-console", line.Metadata[ConsoleLogMetadataKeys.WorkflowInstanceId]);
    }

    [Fact]
    public async Task ConsoleCapture_AttachesAmbientElsaMetadataAtWriteTime()
    {
        await using var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddConsoleLogsServices(options => options.SourceId = "test-source")
            .BuildServiceProvider();
        var contextAccessor = serviceProvider.GetRequiredService<IConsoleLogContextAccessor>();
        var initializer = serviceProvider.GetServices<IShellInitializer>().OfType<ConsoleLogCaptureShellInitializer>().Single();

        await initializer.InitializeAsync(CancellationToken.None);

        try
        {
            var line = $"console-workflow-capture-{Guid.NewGuid():N}";
            using (contextAccessor.PushWorkflowInstanceId("workflow-captured"))
                Console.WriteLine(line);

            var provider = serviceProvider.GetRequiredService<IConsoleLogProvider>();

            await AssertEventuallyAsync(async () =>
            {
                var result = await provider.GetRecentAsync(new()
                {
                    Metadata = new Dictionary<string, string>
                    {
                        [ConsoleLogMetadataKeys.WorkflowInstanceId] = "workflow-captured"
                    },
                    Query = line,
                    Limit = 10
                });

                var capturedLine = Assert.Single(result.Items);
                Assert.Equal("workflow-captured", capturedLine.Metadata[ConsoleLogMetadataKeys.WorkflowInstanceId]);
            });
        }
        finally
        {
            var drainHandler = serviceProvider.GetServices<IDrainHandler>().OfType<ConsoleLogCaptureShellDrainHandler>().Single();
            await drainHandler.DrainAsync(new NoopDrainExtensionHandle(), CancellationToken.None);
        }
    }

    [Fact]
    public async Task AddConsoleLogsServices_DecoratesExistingProvider()
    {
        var innerProvider = new RecordingConsoleLogProvider();
        await using var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConsoleLogProvider>(innerProvider)
            .AddConsoleLogsServices()
            .BuildServiceProvider();
        var contextAccessor = serviceProvider.GetRequiredService<IConsoleLogContextAccessor>();
        var provider = serviceProvider.GetRequiredService<IConsoleLogProvider>();

        using (contextAccessor.PushWorkflowInstanceId("workflow-console"))
            await provider.PublishAsync(new ConsoleLogLine { Text = "message", Source = new ConsoleLogSource { Id = "test-source" } });

        Assert.NotNull(innerProvider.PublishedLine);
        Assert.Equal("workflow-console", innerProvider.PublishedLine.Metadata[ConsoleLogMetadataKeys.WorkflowInstanceId]);
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

    private sealed class RecordingConsoleLogProvider : IConsoleLogProvider
    {
        public ConsoleLogLine? PublishedLine { get; private set; }

        public ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
        {
            PublishedLine = line;
            return ValueTask.CompletedTask;
        }

        public ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RecentConsoleLogsResult());
        }

        public IAsyncEnumerable<ConsoleLogStreamingItem> SubscribeAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
        {
            return AsyncEnumerable.Empty<ConsoleLogStreamingItem>();
        }

        public ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyCollection<ConsoleLogSource>>([]);
        }
    }

    private sealed class NoopDrainExtensionHandle : IDrainExtensionHandle
    {
        public bool TryExtend(TimeSpan requestedExtension, out TimeSpan grantedExtension)
        {
            grantedExtension = TimeSpan.Zero;
            return false;
        }
    }
}
