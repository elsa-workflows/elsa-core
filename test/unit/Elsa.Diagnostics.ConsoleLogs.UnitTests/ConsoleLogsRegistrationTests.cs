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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests;

public class ConsoleLogsRegistrationTests
{
    [Before(Test)]
    public async Task InitializeAsync()
    {
        await ConsoleLogStreamingHost.ShutdownAsync();
        ConsoleStreamHook.Uninstall();
    }

    [After(Test)]
    public async Task DisposeAsync()
    {
        await ConsoleLogStreamingHost.ShutdownAsync();
        ConsoleStreamHook.Uninstall();
    }

    [Test]
    public async Task AddConsoleLogsServices_RegistersConsoleLogPipeline()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConsoleLogsServices();

        await using var serviceProvider = services.BuildServiceProvider();

        await Assert.That(serviceProvider.GetRequiredService<IConsoleLogProvider>()).IsNotNull();
        await Assert.That(serviceProvider.GetRequiredService<IConsoleLogCapture>()).IsNotNull();
        await Assert.That(serviceProvider.GetRequiredService<IElsaConsoleLogHubAuthorizer>()).IsNotNull();
        await Assert.That(serviceProvider.GetRequiredService<ElsaConsoleLogSubscriptionManager>()).IsNotNull();
        await Assert.That(serviceProvider.GetRequiredService<IConsoleLogContextAccessor>()).IsSameReferenceAs(serviceProvider.GetRequiredService<ConsoleLogContextAccessor>());
        await Assert.That(serviceProvider.GetRequiredService<IConsoleLogMetadataAccessor>()).IsSameReferenceAs(serviceProvider.GetRequiredService<ConsoleLogContextAccessor>());
        await Assert.That(serviceProvider.GetServices<IShellInitializer>()).Contains(x => x.GetType() == typeof(ConsoleLogCaptureShellInitializer));
        await Assert.That(serviceProvider.GetServices<IDrainHandler>()).Contains(x => x.GetType() == typeof(ConsoleLogCaptureShellDrainHandler));
        await AssertConsoleLogPipelineContributors(serviceProvider);
    }

    [Test]
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

                await Assert.That(result.Items).Contains(x => x.Text.Contains(line, StringComparison.Ordinal));
            });
        }
        finally
        {
            var drainHandler = serviceProvider.GetServices<IDrainHandler>().OfType<ConsoleLogCaptureShellDrainHandler>().Single();
            await drainHandler.DrainAsync(new NoopDrainExtensionHandle(), CancellationToken.None);
        }
    }

    [Test]
    public async Task AddConsoleLogsHost_RegistersHostedServicesAndCaptureDependencies()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConsoleLogsHost();

        await using var serviceProvider = services.BuildServiceProvider();
        await Assert.That(serviceProvider.GetRequiredService<IConsoleLogCapture>()).IsNotNull();
        await Assert.That(serviceProvider.GetRequiredService<IConsoleLogProvider>()).IsNotNull();
        await Assert.That(serviceProvider.GetRequiredService<IConsoleLogContextAccessor>()).IsSameReferenceAs(serviceProvider.GetRequiredService<ConsoleLogContextAccessor>());
        await Assert.That(serviceProvider.GetRequiredService<IConsoleLogMetadataAccessor>()).IsSameReferenceAs(serviceProvider.GetRequiredService<ConsoleLogContextAccessor>());
        await Assert.That(serviceProvider.GetServices<IHostedService>()).Contains(x => x.GetType().Name == "ConsoleLogCaptureHostedService");
        await AssertConsoleLogPipelineContributors(serviceProvider);
    }

    [Test]
    public async Task AddConsoleLogsHost_AppliesConfiguration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConsoleLogsHost(options => options.RecentCapacity = 17);

        using var serviceProvider = services.BuildServiceProvider();

        await Assert.That(serviceProvider.GetRequiredService<IOptions<ConsoleLogStreaming.Core.Options.ConsoleLogOptions>>().Value.RecentCapacity).IsEqualTo(17);
    }

    [Test]
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

        var line = await Assert.That(result.Items).HasSingleItem();
        await Assert.That(line.Metadata[ConsoleLogMetadataKeys.WorkflowInstanceId]).IsEqualTo("workflow-console");
    }

    [Test]
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

                var capturedLine = await Assert.That(result.Items).HasSingleItem();
                await Assert.That(capturedLine.Metadata[ConsoleLogMetadataKeys.WorkflowInstanceId]).IsEqualTo("workflow-captured");
            });
        }
        finally
        {
            var drainHandler = serviceProvider.GetServices<IDrainHandler>().OfType<ConsoleLogCaptureShellDrainHandler>().Single();
            await drainHandler.DrainAsync(new NoopDrainExtensionHandle(), CancellationToken.None);
        }
    }

    [Test]
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

        await Assert.That(innerProvider.PublishedLine).IsNotNull();
        await Assert.That(innerProvider.PublishedLine.Metadata[ConsoleLogMetadataKeys.WorkflowInstanceId]).IsEqualTo("workflow-console");
    }

    [Test]
    public async Task DecoratedProvider_FiltersRecentRowsByMetadataWhenInnerProviderDoesNot()
    {
        var innerProvider = new MetadataIgnoringConsoleLogProvider();
        await using var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConsoleLogProvider>(innerProvider)
            .AddConsoleLogsServices()
            .BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IConsoleLogProvider>();

        await provider.PublishAsync(CreateLine("workflow-a", "a"));
        await provider.PublishAsync(CreateLine("workflow-b", "b"));

        var result = await provider.GetRecentAsync(new()
        {
            Metadata = new Dictionary<string, string>
            {
                [ConsoleLogMetadataKeys.WorkflowInstanceId] = "workflow-b"
            }
        });

        var line = await Assert.That(result.Items).HasSingleItem();
        await Assert.That(line.Text).IsEqualTo("b");
    }

    [Test]
    public async Task DecoratedProvider_FiltersRecentRowsByBufferedMetadataWhenInnerProviderDropsMetadata()
    {
        var innerProvider = new MetadataDroppingConsoleLogProvider();
        await using var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConsoleLogProvider>(innerProvider)
            .AddConsoleLogsServices()
            .BuildServiceProvider();
        var contextAccessor = serviceProvider.GetRequiredService<IConsoleLogContextAccessor>();
        var provider = serviceProvider.GetRequiredService<IConsoleLogProvider>();

        using (contextAccessor.PushWorkflowInstanceId("workflow-buffered"))
            await provider.PublishAsync(new ConsoleLogLine { Text = "buffered", Source = new ConsoleLogSource { Id = "test-source" } });

        var publishedLine = await Assert.That(innerProvider.PublishedLines).HasSingleItem();
        await Assert.That(publishedLine.Metadata).IsEmpty();

        var result = await provider.GetRecentAsync(new()
        {
            Metadata = new Dictionary<string, string>
            {
                [ConsoleLogMetadataKeys.WorkflowInstanceId] = "workflow-buffered"
            }
        });

        var line = await Assert.That(result.Items).HasSingleItem();
        await Assert.That(line.Text).IsEqualTo("buffered");
        await Assert.That(line.Metadata[ConsoleLogMetadataKeys.WorkflowInstanceId]).IsEqualTo("workflow-buffered");
    }

    [Test]
    public async Task DecoratedProvider_FiltersLiveRowsByMetadataWhenInnerProviderDoesNot()
    {
        var innerProvider = new MetadataIgnoringConsoleLogProvider();
        innerProvider.LiveItems.Add(CreateLine("workflow-a", "a"));
        innerProvider.LiveItems.Add(CreateLine("workflow-b", "b"));

        await using var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConsoleLogProvider>(innerProvider)
            .AddConsoleLogsServices()
            .BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IConsoleLogProvider>();

        var lines = await provider.SubscribeAsync(new()
        {
            Metadata = new Dictionary<string, string>
            {
                [ConsoleLogMetadataKeys.WorkflowInstanceId] = "workflow-b"
            }
        }).Where(x => x.Line != null).Select(x => x.Line!).ToListAsync();

        var line = await Assert.That(lines).HasSingleItem();
        await Assert.That(line.Text).IsEqualTo("b");
    }

    private static async Task AssertConsoleLogPipelineContributors(IServiceProvider serviceProvider)
    {
        await Assert.That(serviceProvider.GetServices<IWorkflowExecutionPipelineContributor>()).Contains(x => x.GetType() == typeof(ConsoleLogWorkflowExecutionPipelineContributor));
        await Assert.That(serviceProvider.GetServices<IActivityExecutionPipelineContributor>()).Contains(x => x.GetType() == typeof(ConsoleLogActivityExecutionPipelineContributor));
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
            catch (TUnit.Assertions.Exceptions.AssertionException e)
            {
                lastException = e;
                await Task.Delay(25);
            }
        }

        if (lastException != null)
            throw lastException;
    }

    private static ConsoleLogLine CreateLine(string workflowInstanceId, string text) => new()
    {
        Text = text,
        Source = new ConsoleLogSource { Id = "test-source" },
        Metadata = new Dictionary<string, string>
        {
            [ConsoleLogMetadataKeys.WorkflowInstanceId] = workflowInstanceId
        }
    };

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

    private sealed class MetadataIgnoringConsoleLogProvider : IConsoleLogProvider
    {
        public List<ConsoleLogLine> RecentItems { get; } = [];
        public List<ConsoleLogLine> LiveItems { get; } = [];

        public ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
        {
            RecentItems.Add(line);
            return ValueTask.CompletedTask;
        }

        public ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RecentConsoleLogsResult { Items = RecentItems });
        }

        public async IAsyncEnumerable<ConsoleLogStreamingItem> SubscribeAsync(
            ConsoleLogFilter filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var line in LiveItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return ConsoleLogStreamingItem.FromLine(line);
                await Task.Yield();
            }
        }

        public ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyCollection<ConsoleLogSource>>([]);
        }
    }

    private sealed class MetadataDroppingConsoleLogProvider : IConsoleLogProvider
    {
        public List<ConsoleLogLine> PublishedLines { get; } = [];

        public ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
        {
            PublishedLines.Add(line with { Metadata = new Dictionary<string, string>() });
            return ValueTask.CompletedTask;
        }

        public ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RecentConsoleLogsResult { Items = PublishedLines });
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
