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
using System.Threading.Tasks;

namespace Elsa.Diagnostics.ConsoleLogs.UnitTests;

public class ConsoleLogContextAccessorTests
{
    private readonly ConsoleLogContextAccessor _accessor = ConsoleLogContextAccessor.Instance;

    [Test]
    public async Task PushMetadata_RestoresNestedCaseInsensitiveMetadata()
    {
        using (_accessor.PushMetadata("Tenant", "tenant-a"))
        {
            await Assert.That(_accessor.GetMetadata()["tenant"]).IsEqualTo("tenant-a");

            using (_accessor.PushMetadata("tenant", "tenant-b"))
                await Assert.That(_accessor.GetMetadata()["TENANT"]).IsEqualTo("tenant-b");

            await Assert.That(_accessor.GetMetadata()["tenant"]).IsEqualTo("tenant-a");
        }

        await Assert.That(_accessor.GetMetadata()).IsEmpty();
    }

    [Test]
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

        await Assert.That(workflowInstanceIds).IsEquivalentTo(Enumerable.Range(0, 20).Select(index => $"workflow-{index}"), TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(_accessor.GetMetadata()).IsEmpty();
    }

    [Test]
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

        var line = await Assert.That(result.Items).HasSingleItem();
        await Assert.That(line.Text).IsEqualTo("duplicate");
        await Assert.That(line.Metadata[ConsoleLogMetadataKeys.WorkflowInstanceId]).IsEqualTo("workflow-b");
    }

    [Test]
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

        await Assert.That(workflowInstanceId).IsEqualTo(context.Id);
        await Assert.That(_accessor.GetMetadata()).IsEmpty();
    }

    [Test]
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

        await Assert.That(workflowInstanceId).IsEqualTo(context.Id);
        await Assert.That(_accessor.GetMetadata()).IsEmpty();
    }

    [Test]
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

        await Assert.That(workflowInstanceId).IsEqualTo(context.Id);
        await Assert.That(_accessor.GetMetadata()).IsEmpty();
    }

    [Test]
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

        await AssertActivityMetadata(context, metadata);
        await Assert.That(_accessor.GetMetadata()).IsEmpty();
    }

    [Test]
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

        await AssertActivityMetadata(context, metadata);
        await Assert.That(_accessor.GetMetadata()).IsEmpty();
    }

    [Test]
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

        await AssertActivityMetadata(context, metadata);
        await Assert.That(_accessor.GetMetadata()).IsEmpty();
    }

    private static async Task AssertActivityMetadata(ActivityExecutionContext context, IReadOnlyDictionary<string, string>? metadata)
    {
        await Assert.That(metadata).IsNotNull();
        await Assert.That(metadata![ConsoleLogMetadataKeys.WorkflowInstanceId]).IsEqualTo(context.WorkflowExecutionContext.Id);
        await Assert.That(metadata[ConsoleLogMetadataKeys.ActivityInstanceId]).IsEqualTo(context.Id);
        await Assert.That(metadata[ConsoleLogMetadataKeys.ActivityId]).IsEqualTo(context.Activity.Id);
        await Assert.That(metadata[ConsoleLogMetadataKeys.ActivityNodeId]).IsEqualTo(context.NodeId);
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
