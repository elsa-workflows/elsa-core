using ConsoleLogStream.Core;
using ConsoleLogStream.Core.Capture;
using ConsoleLogStream.Core.Models;
using Elsa.Diagnostics.ConsoleLogs.Extensions;
using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.RealTime;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests;

public class ConsoleLogsRegistrationTests
{
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
        AssertConsoleLogPipelineContributors(serviceProvider);
    }

    [Fact]
    public void AddConsoleLogsHost_AppliesConfiguration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConsoleLogsHost(options => options.RecentCapacity = 17);

        using var serviceProvider = services.BuildServiceProvider();

        Assert.Equal(17, serviceProvider.GetRequiredService<IOptions<ConsoleLogStream.Core.Options.ConsoleLogOptions>>().Value.RecentCapacity);
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

        public IAsyncEnumerable<ConsoleLogStreamItem> SubscribeAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
        {
            return AsyncEnumerable.Empty<ConsoleLogStreamItem>();
        }

        public ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyCollection<ConsoleLogSource>>([]);
        }
    }
}
