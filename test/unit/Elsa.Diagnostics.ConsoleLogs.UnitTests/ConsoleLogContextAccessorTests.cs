using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.DependencyInjection;
using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests;

[Collection(ConsoleHostStateCollection.Name)]
public class ConsoleLogContextAccessorTests
{
    private readonly ConsoleLogContextAccessor _accessor = ConsoleLogContextAccessor.Instance;

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
            .AddConsoleLogStream(options => options.SourceId = "test-source")
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
    public async Task WorkflowExecutionMiddleware_PushesWorkflowInstanceMetadata()
    {
        await using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConsoleLogContextAccessor>(_accessor)
            .BuildServiceProvider();
        var activityExecutionContext = await new ActivityTestFixture(new TestActivity()).BuildAsync();
        var context = activityExecutionContext.WorkflowExecutionContext;
        string? workflowInstanceId = null;
        var pipeline = new WorkflowExecutionPipeline(serviceProvider, builder => builder
            .UseMiddleware<ConsoleLogWorkflowExecutionMiddleware>()
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
    public async Task WorkflowExecutionContributor_PushesWorkflowInstanceMetadata()
    {
        await using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConsoleLogContextAccessor>(_accessor)
            .BuildServiceProvider();
        var activityExecutionContext = await new ActivityTestFixture(new TestActivity()).BuildAsync();
        var context = activityExecutionContext.WorkflowExecutionContext;
        string? workflowInstanceId = null;
        var pipeline = new WorkflowExecutionPipeline(serviceProvider, builder =>
        {
            new ConsoleLogWorkflowExecutionPipelineContributor().Configure(builder);
            builder.Use(_ => executionContext =>
            {
                workflowInstanceId = _accessor.GetMetadata()[ConsoleLogMetadataKeys.WorkflowInstanceId];
                return ValueTask.CompletedTask;
            });
        });

        await pipeline.ExecuteAsync(context);

        Assert.Equal(context.Id, workflowInstanceId);
        Assert.Empty(_accessor.GetMetadata());
    }

    [Fact]
    public async Task WorkflowFeature_AppliesWorkflowExecutionContributors()
    {
        var activityExecutionContext = await new ActivityTestFixture(new TestActivity()).BuildAsync();
        var context = activityExecutionContext.WorkflowExecutionContext;
        string? workflowInstanceId = null;
        var feature = new Elsa.Workflows.ShellFeatures.WorkflowsFeature
        {
            WorkflowExecutionPipeline = builder => builder.Use(_ => executionContext =>
            {
                workflowInstanceId = _accessor.GetMetadata()[ConsoleLogMetadataKeys.WorkflowInstanceId];
                return ValueTask.CompletedTask;
            })
        };
        var services = new ServiceCollection()
            .AddSingleton<IConsoleLogContextAccessor>(_accessor)
            .AddScoped<IWorkflowExecutionPipelineContributor, ConsoleLogWorkflowExecutionPipelineContributor>();
        feature.ConfigureServices(services);
        await using var serviceProvider = services.BuildServiceProvider();
        var pipeline = serviceProvider.GetRequiredService<IWorkflowExecutionPipeline>();

        await pipeline.ExecuteAsync(context);

        Assert.Equal(context.Id, workflowInstanceId);
        Assert.Empty(_accessor.GetMetadata());
    }

    [Fact]
    public async Task ActivityExecutionMiddleware_PushesWorkflowInstanceMetadata()
    {
        await using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConsoleLogContextAccessor>(_accessor)
            .BuildServiceProvider();
        var context = await CreateActivityContextAsync();
        IReadOnlyDictionary<string, string>? metadata = null;
        var pipeline = new ActivityExecutionPipeline(serviceProvider, builder => builder
            .UseMiddleware<ConsoleLogActivityExecutionMiddleware>()
            .Use(_ => executionContext =>
            {
                metadata = _accessor.GetMetadata();
                return ValueTask.CompletedTask;
            }));

        await pipeline.ExecuteAsync(context);

        AssertActivityMetadata(context, metadata);
        Assert.Empty(_accessor.GetMetadata());
    }

    [Fact]
    public async Task ActivityExecutionContributor_PushesWorkflowInstanceMetadata()
    {
        await using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConsoleLogContextAccessor>(_accessor)
            .BuildServiceProvider();
        var context = await CreateActivityContextAsync();
        IReadOnlyDictionary<string, string>? metadata = null;
        var pipeline = new ActivityExecutionPipeline(serviceProvider, builder =>
        {
            new ConsoleLogActivityExecutionPipelineContributor().Configure(builder);
            builder.Use(_ => executionContext =>
            {
                metadata = _accessor.GetMetadata();
                return ValueTask.CompletedTask;
            });
        });

        await pipeline.ExecuteAsync(context);

        AssertActivityMetadata(context, metadata);
        Assert.Empty(_accessor.GetMetadata());
    }

    [Fact]
    public async Task WorkflowFeature_AppliesActivityExecutionContributors()
    {
        var context = await CreateActivityContextAsync();
        IReadOnlyDictionary<string, string>? metadata = null;
        var feature = new Elsa.Workflows.ShellFeatures.WorkflowsFeature
        {
            ActivityExecutionPipeline = builder => builder.Use(_ => executionContext =>
            {
                metadata = _accessor.GetMetadata();
                return ValueTask.CompletedTask;
            })
        };
        var services = new ServiceCollection()
            .AddSingleton<IConsoleLogContextAccessor>(_accessor)
            .AddScoped<IActivityExecutionPipelineContributor, ConsoleLogActivityExecutionPipelineContributor>();
        feature.ConfigureServices(services);
        await using var serviceProvider = services.BuildServiceProvider();
        var pipeline = serviceProvider.GetRequiredService<IActivityExecutionPipeline>();

        await pipeline.ExecuteAsync(context);

        AssertActivityMetadata(context, metadata);
        Assert.Empty(_accessor.GetMetadata());
    }

    private static void AssertActivityMetadata(ActivityExecutionContext context, IReadOnlyDictionary<string, string>? metadata)
    {
        Assert.NotNull(metadata);
        Assert.Equal(context.WorkflowExecutionContext.Id, metadata[ConsoleLogMetadataKeys.WorkflowInstanceId]);
        Assert.Equal(context.Id, metadata[ConsoleLogMetadataKeys.ActivityInstanceId]);
        Assert.Equal(context.Activity.Id, metadata[ConsoleLogMetadataKeys.ActivityId]);
        Assert.Equal(context.NodeId, metadata[ConsoleLogMetadataKeys.ActivityNodeId]);
    }

    private static async Task<ActivityExecutionContext> CreateActivityContextAsync()
    {
        var context = await new ActivityTestFixture(new TestActivity()).BuildAsync();
        context.Id = "activity-instance-a";
        return context;
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

    private class TestActivity : CodeActivity
    {
        public TestActivity()
        {
            Id = "activity-a";
            NodeId = "node-a";
        }

        protected override void Execute(ActivityExecutionContext context)
        {
        }
    }
}
