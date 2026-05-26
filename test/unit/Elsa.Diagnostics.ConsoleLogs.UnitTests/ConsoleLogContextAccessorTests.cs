using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.Capture;
using ConsoleLogStreaming.Core.DependencyInjection;
using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Extensions;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests;

[Collection(ConsoleHostStateCollection.Name)]
public class ConsoleLogContextAccessorTests : IAsyncLifetime
{
    private readonly ConsoleLogContextAccessor _accessor = ConsoleLogContextAccessor.Instance;

    public Task InitializeAsync()
    {
        ConsoleStreamHook.Uninstall();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        ConsoleStreamHook.Uninstall();
        return Task.CompletedTask;
    }

    [Fact]
    public void PushMetadata_RestoresNestedCaseInsensitiveMetadata()
    {
        using (_accessor.PushMetadata("Tenant", "tenant-a"))
        {
            Assert.Equal("tenant-a", _accessor.GetMetadata()["tenant"]);

            using (_accessor.PushMetadata("tenant", "tenant-b"))
                Assert.Equal("tenant-b", _accessor.GetMetadata()["TENANT"]);

            Assert.Equal("tenant-a", _accessor.GetMetadata()["tenant"]);
        }

        Assert.Empty(_accessor.GetMetadata());
    }

    [Fact]
    public async Task PushWorkflowInstanceId_IsIsolatedAcrossConcurrentAsyncFlows()
    {
        var tasks = Enumerable.Range(0, 20).Select(index => Task.Run(async () =>
        {
            var workflowInstanceId = $"workflow-{index}";

            using (_accessor.PushWorkflowInstanceId(workflowInstanceId))
            {
                await Task.Yield();
                await Task.Delay(1);
                return _accessor.GetMetadata()[ConsoleLogMetadataKeys.WorkflowInstanceId];
            }
        }));

        var workflowInstanceIds = await Task.WhenAll(tasks);

        Assert.Equal(Enumerable.Range(0, 20).Select(index => $"workflow-{index}"), workflowInstanceIds);
        Assert.Empty(_accessor.GetMetadata());
    }

    [Fact]
    public async Task RecentQuery_FiltersDuplicateConsoleLinesByWorkflowMetadata()
    {
        await using var serviceProvider = new ServiceCollection()
            .AddConsoleLogStreaming(options => options.SourceId = "test-source")
            .BuildServiceProvider();

        var provider = serviceProvider.GetRequiredService<IConsoleLogProvider>();
        await provider.PublishAsync(CreateLine("duplicate", "workflow-a", 1));
        await provider.PublishAsync(CreateLine("duplicate", "workflow-b", 2));

        var result = await provider.GetRecentAsync(new ConsoleLogStreaming.Core.Models.ConsoleLogFilter
        {
            Metadata = new Dictionary<string, string>
            {
                [ConsoleLogMetadataKeys.WorkflowInstanceId] = "workflow-b"
            }
        });

        var line = Assert.Single(result.Items);
        Assert.Equal("duplicate", line.Text);
        Assert.Equal("workflow-b", line.Metadata[ConsoleLogMetadataKeys.WorkflowInstanceId]);
    }

    [Fact]
    public void ConsoleLineBuffer_PreservesWorkflowMetadataForMultilineOutput()
    {
        var buffer = new ConsoleLineBuffer(Microsoft.Extensions.Options.Options.Create(new ConsoleLogStreaming.Core.Options.ConsoleLogOptions()));
        IReadOnlyCollection<BufferedConsoleLine> lines;

        using (_accessor.PushWorkflowInstanceId("workflow-multiline"))
            lines = buffer.Append("first line\nsecond line\n", DateTimeOffset.UtcNow, _accessor.GetMetadata());

        Assert.Collection(
            lines,
            line => Assert.Equal("workflow-multiline", line.Metadata[ConsoleLogMetadataKeys.WorkflowInstanceId]),
            line => Assert.Equal("workflow-multiline", line.Metadata[ConsoleLogMetadataKeys.WorkflowInstanceId]));
    }

    [Fact]
    public async Task ConsoleCapture_AttachesWorkflowMetadataToNonLoggerConsoleWrites()
    {
        await using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConsoleLogMetadataAccessor>(_accessor)
            .AddConsoleLogStreaming(options => options.SourceId = "test-source")
            .BuildServiceProvider();

        var capture = serviceProvider.GetRequiredService<IConsoleLogCapture>();
        await capture.StartAsync();

        using (_accessor.PushWorkflowInstanceId("workflow-console"))
            Console.WriteLine("plain console output");

        await capture.StopAsync();

        var result = await serviceProvider.GetRequiredService<IConsoleLogProvider>().GetRecentAsync(new ConsoleLogStreaming.Core.Models.ConsoleLogFilter
        {
            Metadata = new Dictionary<string, string>
            {
                [ConsoleLogMetadataKeys.WorkflowInstanceId] = "workflow-console"
            }
        });

        var line = Assert.Single(result.Items, x => x.Text == "plain console output");
        Assert.Equal("workflow-console", line.Metadata[ConsoleLogMetadataKeys.WorkflowInstanceId]);
    }

    [Fact]
    public async Task WorkflowExecutionPipeline_PushesWorkflowInstanceMetadata()
    {
        var activityExecutionContext = await new ActivityTestFixture(new TestActivity()).BuildAsync();
        var context = activityExecutionContext.WorkflowExecutionContext;
        var inner = new CapturingWorkflowExecutionPipeline(_accessor);
        var pipeline = new ConsoleLogWorkflowExecutionPipeline(inner, _accessor);

        await pipeline.ExecuteAsync(context);

        Assert.Equal(context.Id, inner.WorkflowInstanceId);
        Assert.Empty(_accessor.GetMetadata());
    }

    [Fact]
    public async Task WorkflowPipelineBuilderMiddleware_PushesWorkflowInstanceMetadata()
    {
        await using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConsoleLogContextAccessor>(_accessor)
            .BuildServiceProvider();
        var activityExecutionContext = await new ActivityTestFixture(new TestActivity()).BuildAsync();
        var context = activityExecutionContext.WorkflowExecutionContext;
        string? workflowInstanceId = null;
        var pipeline = new WorkflowExecutionPipeline(serviceProvider, builder => builder
            .UseConsoleLogContext()
            .Use(_ => executionContext =>
            {
                workflowInstanceId = _accessor.GetMetadata()[ConsoleLogMetadataKeys.WorkflowInstanceId];
                return ValueTask.CompletedTask;
            }));

        await pipeline.ExecuteAsync(context);

        Assert.Equal(context.Id, workflowInstanceId);
        Assert.Empty(_accessor.GetMetadata());
    }

    [Fact]
    public async Task ActivityExecutionPipeline_PushesWorkflowInstanceMetadata()
    {
        var context = await new ActivityTestFixture(new TestActivity()).BuildAsync();
        var inner = new CapturingActivityExecutionPipeline(_accessor);
        var pipeline = new ConsoleLogActivityExecutionPipeline(inner, _accessor);

        await pipeline.ExecuteAsync(context);

        Assert.Equal(context.WorkflowExecutionContext.Id, inner.WorkflowInstanceId);
        Assert.Empty(_accessor.GetMetadata());
    }

    [Fact]
    public async Task ActivityPipelineBuilderMiddleware_PushesWorkflowInstanceMetadata()
    {
        await using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConsoleLogContextAccessor>(_accessor)
            .BuildServiceProvider();
        var context = await new ActivityTestFixture(new TestActivity()).BuildAsync();
        string? workflowInstanceId = null;
        var pipeline = new ActivityExecutionPipeline(serviceProvider, builder => builder
            .UseConsoleLogContext()
            .Use(_ => executionContext =>
            {
                workflowInstanceId = _accessor.GetMetadata()[ConsoleLogMetadataKeys.WorkflowInstanceId];
                return ValueTask.CompletedTask;
            }));

        await pipeline.ExecuteAsync(context);

        Assert.Equal(context.WorkflowExecutionContext.Id, workflowInstanceId);
        Assert.Empty(_accessor.GetMetadata());
    }

    private static ConsoleLogStreaming.Core.Models.ConsoleLogLine CreateLine(string text, string workflowInstanceId, long sequence) => new()
    {
        Text = text,
        Sequence = sequence,
        Stream = ConsoleLogStreaming.Core.Models.ConsoleStream.Stdout,
        Source = new ConsoleLogStreaming.Core.Models.ConsoleLogSource { Id = "test-source" },
        Metadata = new Dictionary<string, string>
        {
            [ConsoleLogMetadataKeys.WorkflowInstanceId] = workflowInstanceId
        }
    };

    private sealed class CapturingWorkflowExecutionPipeline(IConsoleLogContextAccessor accessor) : IWorkflowExecutionPipeline
    {
        public string? WorkflowInstanceId { get; private set; }
        public Action<IWorkflowExecutionPipelineBuilder> ConfigurePipelineBuilder => _ => { };
        public WorkflowMiddlewareDelegate Pipeline => _ => ValueTask.CompletedTask;
        public WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionPipelineBuilder> setup) => Pipeline;

        public Task ExecuteAsync(WorkflowExecutionContext context)
        {
            WorkflowInstanceId = accessor.GetMetadata()[ConsoleLogMetadataKeys.WorkflowInstanceId];
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingActivityExecutionPipeline(IConsoleLogContextAccessor accessor) : IActivityExecutionPipeline
    {
        public string? WorkflowInstanceId { get; private set; }
        public ActivityMiddlewareDelegate Pipeline => _ => ValueTask.CompletedTask;
        public ActivityMiddlewareDelegate Setup(Action<IActivityExecutionPipelineBuilder> setup) => Pipeline;

        public Task ExecuteAsync(ActivityExecutionContext context)
        {
            WorkflowInstanceId = accessor.GetMetadata()[ConsoleLogMetadataKeys.WorkflowInstanceId];
            return Task.CompletedTask;
        }
    }

    private class TestActivity : CodeActivity
    {
        protected override void Execute(ActivityExecutionContext context)
        {
        }
    }
}
