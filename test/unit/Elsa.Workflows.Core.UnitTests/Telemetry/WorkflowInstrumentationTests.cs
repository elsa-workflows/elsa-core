using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Elsa.Common;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Middleware.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Options;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Services;
using Elsa.Workflows.State;
using Elsa.Workflows.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Workflows.Core.UnitTests.Telemetry;

using DiagnosticsActivity = System.Diagnostics.Activity;

[Collection(nameof(WorkflowInstrumentationTestCollection))]
public class WorkflowInstrumentationTests
{
    [Fact]
    public async Task ActivityInvoker_Should_Emit_Activity_Span_And_Duration_Metric()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var activity = new TestActivity { Name = "Test activity" };
        var context = await new ActivityTestFixture(activity).BuildAsync();
        var invoker = new ActivityInvoker(new CompletingActivityExecutionPipeline(), new ActivityLoggerStateGenerator(), NullLogger<ActivityInvoker>.Instance);

        await invoker.InvokeAsync(context);

        var span = Assert.Single(activityCapture.StoppedActivities);
        var tags = span.TagObjects.ToDictionary(x => x.Key, x => x.Value);
        Assert.Equal("activity.execute", span.OperationName);
        Assert.Equal(activity.Type, tags[WorkflowInstrumentation.ActivityType]);
        Assert.Equal(context.WorkflowExecutionContext.Workflow.Identity.Id, tags[WorkflowInstrumentation.WorkflowDefinitionVersionId]);
        Assert.Equal(ActivityStatus.Completed.ToString(), tags[WorkflowInstrumentation.ActivityStatus]);
        var activityDuration = Assert.Single(meterCapture.DoubleMeasurements, x => x.InstrumentName == "elsa.activity.duration" && x.Value >= 0);
        Assert.False(activityDuration.Tags.ContainsKey(WorkflowInstrumentation.WorkflowDefinitionVersionId));
        Assert.False(activityDuration.Tags.ContainsKey(WorkflowInstrumentation.ActivityName));
        Assert.Equal(false, activityDuration.Tags[WorkflowInstrumentation.ActivityFaulted]);
    }

    [Fact]
    public async Task ActivityInvoker_Should_Not_Record_Faulted_Metric_When_Pipeline_Cancels()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var context = await new ActivityTestFixture(new TestActivity()).BuildAsync();
        var invoker = new ActivityInvoker(new CancellingActivityExecutionPipeline(), new ActivityLoggerStateGenerator(), NullLogger<ActivityInvoker>.Instance);

        await Assert.ThrowsAsync<OperationCanceledException>(() => invoker.InvokeAsync(context));

        var span = Assert.Single(activityCapture.StoppedActivities);
        Assert.Equal(ActivityStatusCode.Ok, span.Status);
        var activityDuration = Assert.Single(meterCapture.DoubleMeasurements, x => x.InstrumentName == "elsa.activity.duration" && x.Value >= 0);
        Assert.Equal(false, activityDuration.Tags[WorkflowInstrumentation.ActivityFaulted]);
        Assert.Equal(ActivityStatus.Canceled.ToString(), activityDuration.Tags[WorkflowInstrumentation.ActivityStatus]);
    }

    [Fact]
    public async Task WorkflowRunner_Should_Record_Completed_Metric_When_Pipeline_Finishes()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var activityExecutionContext = await new ActivityTestFixture(new TestActivity()).BuildAsync();
        var context = activityExecutionContext.WorkflowExecutionContext;
        context.Workflow.WorkflowMetadata = new("Test workflow");
        context.ParentWorkflowInstanceId = "parent-instance-id";
        var runner = CreateWorkflowRunner(context, new CompletingWorkflowExecutionPipeline());

        await runner.RunAsync(context);

        var span = Assert.Single(activityCapture.StoppedActivities);
        var tags = span.TagObjects.ToDictionary(x => x.Key, x => x.Value);
        Assert.Equal(ActivityStatusCode.Ok, span.Status);
        Assert.Equal(context.Workflow.Identity.Id, tags[WorkflowInstrumentation.WorkflowDefinitionVersionId]);
        Assert.True(tags.ContainsKey(WorkflowInstrumentation.WorkflowSubStatus));
        Assert.Equal(WorkflowSubStatus.Finished.ToString(), tags[WorkflowInstrumentation.WorkflowSubStatus]);
        Assert.Equal("parent-instance-id", tags[WorkflowInstrumentation.WorkflowParentInstanceId]);
        Assert.False(tags.ContainsKey("workflow.parent_instance.id"));
        var started = Assert.Single(meterCapture.LongMeasurements, x => x.InstrumentName == "elsa.workflow.started" && x.Value == 1);
        var completed = Assert.Single(meterCapture.LongMeasurements, x => x.InstrumentName == "elsa.workflow.completed" && x.Value == 1);
        Assert.False(started.Tags.ContainsKey(WorkflowInstrumentation.WorkflowDefinitionVersionId));
        Assert.False(completed.Tags.ContainsKey(WorkflowInstrumentation.WorkflowDefinitionVersionId));
        Assert.False(started.Tags.ContainsKey(WorkflowInstrumentation.WorkflowSubStatus));
        Assert.Equal(WorkflowSubStatus.Finished.ToString(), completed.Tags[WorkflowInstrumentation.WorkflowSubStatus]);
    }

    [Fact]
    public async Task WorkflowRunner_Should_Record_Faulted_Span_And_Metric_When_Pipeline_Throws()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var activityExecutionContext = await new ActivityTestFixture(new TestActivity()).BuildAsync();
        var context = activityExecutionContext.WorkflowExecutionContext;
        context.Workflow.WorkflowMetadata = new("Test workflow");
        var runner = CreateWorkflowRunner(context, new ThrowingWorkflowExecutionPipeline());

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(context));

        var span = Assert.Single(activityCapture.StoppedActivities);
        var tags = span.TagObjects.ToDictionary(x => x.Key, x => x.Value);
        Assert.Equal("workflow.execute", span.OperationName);
        Assert.Equal(ActivityStatusCode.Error, span.Status);
        Assert.Equal(context.Id, tags[WorkflowInstrumentation.WorkflowInstanceId]);
        Assert.Equal(typeof(InvalidOperationException).FullName, tags[WorkflowInstrumentation.ErrorType]);
        var started = Assert.Single(meterCapture.LongMeasurements, x => x.InstrumentName == "elsa.workflow.started" && x.Value == 1);
        var faulted = Assert.Single(meterCapture.LongMeasurements, x => x.InstrumentName == "elsa.workflow.faulted" && x.Value == 1);
        Assert.False(started.Tags.ContainsKey(WorkflowInstrumentation.WorkflowDefinitionVersionId));
        Assert.False(faulted.Tags.ContainsKey(WorkflowInstrumentation.WorkflowDefinitionVersionId));
        Assert.Equal("Test workflow", started.Tags[WorkflowInstrumentation.WorkflowName]);
        Assert.Equal("Test workflow", faulted.Tags[WorkflowInstrumentation.WorkflowName]);
    }

    [Fact]
    public async Task WorkflowRunner_Should_Not_Record_Faulted_Span_Or_Metric_When_Pipeline_Cancels()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var activityExecutionContext = await new ActivityTestFixture(new TestActivity()).BuildAsync();
        var context = activityExecutionContext.WorkflowExecutionContext;
        var runner = CreateWorkflowRunner(context, new CancellingWorkflowExecutionPipeline());

        await Assert.ThrowsAsync<OperationCanceledException>(() => runner.RunAsync(context));

        var span = Assert.Single(activityCapture.StoppedActivities);
        var tags = span.TagObjects.ToDictionary(x => x.Key, x => x.Value);
        Assert.Equal(ActivityStatusCode.Ok, span.Status);
        Assert.Equal(WorkflowSubStatus.Cancelled.ToString(), tags[WorkflowInstrumentation.WorkflowSubStatus]);
        Assert.Equal(false, tags[WorkflowInstrumentation.WorkflowFaulted]);
        Assert.DoesNotContain(meterCapture.LongMeasurements, x => x.InstrumentName == "elsa.workflow.faulted");
    }

    [Fact]
    public async Task ActivityInvoker_Should_Not_Replace_Pipeline_Exception_When_Outcome_Is_Null()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var context = await new ActivityTestFixture(new TestActivity()).BuildAsync();
        var invoker = new ActivityInvoker(new ThrowingActivityExecutionPipeline(), new ActivityLoggerStateGenerator(), NullLogger<ActivityInvoker>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => invoker.InvokeAsync(context));

        Assert.Equal("Pipeline failed", exception.Message);
        var activityDuration = Assert.Single(meterCapture.DoubleMeasurements, x => x.InstrumentName == "elsa.activity.duration" && x.Value >= 0);
        Assert.Equal(true, activityDuration.Tags[WorkflowInstrumentation.ActivityFaulted]);
        Assert.Equal(ActivityStatus.Faulted.ToString(), activityDuration.Tags[WorkflowInstrumentation.ActivityStatus]);
    }

    [Fact]
    public async Task ActivityInvoker_Should_Not_Emit_ErrorType_When_Faulted_Without_Exception()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var context = await new ActivityTestFixture(new TestActivity()).BuildAsync();
        var invoker = new ActivityInvoker(new FaultingActivityExecutionPipeline(), new ActivityLoggerStateGenerator(), NullLogger<ActivityInvoker>.Instance);

        await invoker.InvokeAsync(context);

        var span = Assert.Single(activityCapture.StoppedActivities);
        var tags = span.TagObjects.ToDictionary(x => x.Key, x => x.Value);
        Assert.Equal(ActivityStatusCode.Error, span.Status);
        Assert.False(tags.ContainsKey(WorkflowInstrumentation.ErrorType));
    }

    private static WorkflowRunner CreateWorkflowRunner(WorkflowExecutionContext context, IWorkflowExecutionPipeline pipeline)
    {
        var notificationSender = Substitute.For<INotificationSender>();
        notificationSender
            .SendAsync(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var workflowStateExtractor = Substitute.For<IWorkflowStateExtractor>();
        workflowStateExtractor.Extract(context).Returns(new WorkflowState
        {
            Id = context.Id,
            DefinitionId = context.Workflow.Identity.DefinitionId,
            DefinitionVersionId = context.Workflow.Identity.Id,
            DefinitionVersion = context.Workflow.Identity.Version,
            Status = context.Status,
            SubStatus = context.SubStatus
        });

        return new(
            context.ServiceProvider,
            pipeline,
            workflowStateExtractor,
            Substitute.For<IWorkflowBuilderFactory>(),
            Substitute.For<IWorkflowGraphBuilder>(),
            Substitute.For<IIdentityGenerator>(),
            notificationSender,
            new WorkflowLoggerStateGenerator(),
            Substitute.For<ICommitStateHandler>(),
            NullLogger<WorkflowRunner>.Instance);
    }

    private sealed class CompletingActivityExecutionPipeline : IActivityExecutionPipeline
    {
        public ActivityMiddlewareDelegate Pipeline => _ => ValueTask.CompletedTask;

        public ActivityMiddlewareDelegate Setup(Action<IActivityExecutionPipelineBuilder> setup) => Pipeline;

        public async Task ExecuteAsync(ActivityExecutionContext context)
        {
            context.TransitionTo(ActivityStatus.Running);
            await context.CompleteActivityAsync();
        }
    }

    private sealed class ThrowingActivityExecutionPipeline : IActivityExecutionPipeline
    {
        public ActivityMiddlewareDelegate Pipeline => _ => ValueTask.CompletedTask;

        public ActivityMiddlewareDelegate Setup(Action<IActivityExecutionPipelineBuilder> setup) => Pipeline;

        public Task ExecuteAsync(ActivityExecutionContext context)
        {
            context.JournalData["Outcomes"] = null!;
            throw new InvalidOperationException("Pipeline failed");
        }
    }

    private sealed class CancellingActivityExecutionPipeline : IActivityExecutionPipeline
    {
        public ActivityMiddlewareDelegate Pipeline => _ => ValueTask.CompletedTask;

        public ActivityMiddlewareDelegate Setup(Action<IActivityExecutionPipelineBuilder> setup) => Pipeline;

        public Task ExecuteAsync(ActivityExecutionContext context)
        {
            context.TransitionTo(ActivityStatus.Canceled);
            throw new OperationCanceledException();
        }
    }

    private sealed class CompletingWorkflowExecutionPipeline : IWorkflowExecutionPipeline
    {
        public Action<IWorkflowExecutionPipelineBuilder> ConfigurePipelineBuilder => _ => { };
        public WorkflowMiddlewareDelegate Pipeline => _ => ValueTask.CompletedTask;

        public WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionPipelineBuilder> setup) => Pipeline;

        public async Task ExecuteAsync(WorkflowExecutionContext context)
        {
            context.ScheduleWorkflow();

            var middleware = new DefaultActivitySchedulerMiddleware(
                _ => ValueTask.CompletedTask,
                new CompletingActivityInvoker(),
                Substitute.For<ICommitStrategyRegistry>(),
                Microsoft.Extensions.Options.Options.Create(new CommitStateOptions()));

            await middleware.InvokeAsync(context);
        }
    }

    private sealed class CompletingActivityInvoker : IActivityInvoker
    {
        public async Task<ActivityExecutionContext> InvokeAsync(WorkflowExecutionContext workflowExecutionContext, IActivity activity, ActivityInvocationOptions? options = null)
        {
            var activityExecutionContext = options?.ExistingActivityExecutionContext ?? await workflowExecutionContext.CreateActivityExecutionContextAsync(activity, options);

            if (!workflowExecutionContext.ActivityExecutionContexts.Any(x => x.Id == activityExecutionContext.Id))
                workflowExecutionContext.AddActivityExecutionContext(activityExecutionContext);

            await InvokeAsync(activityExecutionContext);
            return activityExecutionContext;
        }

        public async Task InvokeAsync(ActivityExecutionContext activityExecutionContext)
        {
            activityExecutionContext.TransitionTo(ActivityStatus.Running);
            await activityExecutionContext.CompleteActivityAsync();
        }
    }

    private sealed class ThrowingWorkflowExecutionPipeline : IWorkflowExecutionPipeline
    {
        public Action<IWorkflowExecutionPipelineBuilder> ConfigurePipelineBuilder => _ => { };
        public WorkflowMiddlewareDelegate Pipeline => _ => ValueTask.CompletedTask;

        public WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionPipelineBuilder> setup) => Pipeline;

        public Task ExecuteAsync(WorkflowExecutionContext context) => throw new InvalidOperationException("Pipeline failed");
    }

    private sealed class CancellingWorkflowExecutionPipeline : IWorkflowExecutionPipeline
    {
        public Action<IWorkflowExecutionPipelineBuilder> ConfigurePipelineBuilder => _ => { };
        public WorkflowMiddlewareDelegate Pipeline => _ => ValueTask.CompletedTask;

        public WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionPipelineBuilder> setup) => Pipeline;

        public Task ExecuteAsync(WorkflowExecutionContext context)
        {
            context.Cancel();
            throw new OperationCanceledException();
        }
    }

    private sealed class TestActivity : Elsa.Workflows.Activity
    {
    }

    private sealed class ActivityCapture : IDisposable
    {
        private readonly ActivityListener _listener;

        public ActivityCapture()
        {
            _listener = new()
            {
                ShouldListenTo = source => source.Name == WorkflowInstrumentation.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => StoppedActivities.Enqueue(activity)
            };

            ActivitySource.AddActivityListener(_listener);
        }

        public ConcurrentQueue<DiagnosticsActivity> StoppedActivities { get; } = new();

        public void Dispose() => _listener.Dispose();
    }

    private sealed class MeterCapture : IDisposable
    {
        private readonly MeterListener _listener = new();

        public MeterCapture()
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == WorkflowInstrumentation.MeterName)
                    listener.EnableMeasurementEvents(instrument);
            };

            _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) => LongMeasurements.Enqueue(new(instrument.Name, measurement, CaptureTags(tags))));
            _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) => DoubleMeasurements.Enqueue(new(instrument.Name, measurement, CaptureTags(tags))));
            _listener.Start();
        }

        public ConcurrentQueue<Measurement<long>> LongMeasurements { get; } = new();
        public ConcurrentQueue<Measurement<double>> DoubleMeasurements { get; } = new();

        public void Dispose() => _listener.Dispose();

        private static IReadOnlyDictionary<string, object?> CaptureTags(ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            var result = new Dictionary<string, object?>();

            foreach (var tag in tags)
                result[tag.Key] = tag.Value;

            return result;
        }
    }

    private sealed class FaultingActivityExecutionPipeline : IActivityExecutionPipeline
    {
        public ActivityMiddlewareDelegate Pipeline => _ => ValueTask.CompletedTask;

        public ActivityMiddlewareDelegate Setup(Action<IActivityExecutionPipelineBuilder> setup) => Pipeline;

        public Task ExecuteAsync(ActivityExecutionContext context)
        {
            context.TransitionTo(ActivityStatus.Faulted);
            return Task.CompletedTask;
        }
    }

    private readonly record struct Measurement<T>(string InstrumentName, T Value, IReadOnlyDictionary<string, object?> Tags);
}

[CollectionDefinition(nameof(WorkflowInstrumentationTestCollection), DisableParallelization = true)]
public sealed class WorkflowInstrumentationTestCollection
{
}
