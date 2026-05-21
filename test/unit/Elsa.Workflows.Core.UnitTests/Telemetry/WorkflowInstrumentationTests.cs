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

        var span = GetStoppedActivity(activityCapture, "activity.execute", WorkflowInstrumentation.ActivityExecutionId, context.Id);
        Assert.Equal("activity.execute", span.OperationName);
        Assert.Equal(activity.Type, GetTag(span.TagObjects, WorkflowInstrumentation.ActivityType));
        Assert.Equal(context.WorkflowExecutionContext.Workflow.Identity.Id, GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowDefinitionVersionId));
        Assert.Equal(ActivityStatus.Completed.ToString(), GetTag(span.TagObjects, WorkflowInstrumentation.ActivityStatus));
        var activityDuration = GetActivityDuration(meterCapture, context);
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

        var span = GetStoppedActivity(activityCapture, "activity.execute", WorkflowInstrumentation.ActivityExecutionId, context.Id);
        Assert.Equal(ActivityStatusCode.Ok, span.Status);
        var activityDuration = GetActivityDuration(meterCapture, context);
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

        var span = GetStoppedActivity(activityCapture, "workflow.execute", WorkflowInstrumentation.WorkflowInstanceId, context.Id);
        Assert.Equal(ActivityStatusCode.Ok, span.Status);
        Assert.Equal(context.Workflow.Identity.Id, GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowDefinitionVersionId));
        var workflowSubStatus = GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowSubStatus);
        Assert.Equal(WorkflowSubStatus.Finished.ToString(), workflowSubStatus);
        Assert.Equal("parent-instance-id", GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowParentInstanceId));
        Assert.False(HasTag(span.TagObjects, "workflow.parent_instance.id"));
        var started = GetWorkflowMeasurement(meterCapture, "elsa.workflow.started", context);
        var completed = GetWorkflowMeasurement(meterCapture, "elsa.workflow.completed", context);
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

        var span = GetStoppedActivity(activityCapture, "workflow.execute", WorkflowInstrumentation.WorkflowInstanceId, context.Id);
        Assert.Equal("workflow.execute", span.OperationName);
        Assert.Equal(ActivityStatusCode.Error, span.Status);
        Assert.Equal(context.Id, GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowInstanceId));
        Assert.Equal(typeof(InvalidOperationException).FullName, GetTag(span.TagObjects, WorkflowInstrumentation.ExceptionType));
        Assert.False(HasTag(span.TagObjects, "exception.message"));
        Assert.False(HasTag(span.TagObjects, "exception.stacktrace"));
        var exceptionEvent = Assert.Single(span.Events, x => x.Name == "exception");
        Assert.Equal(typeof(InvalidOperationException).FullName, GetTag(exceptionEvent.Tags, WorkflowInstrumentation.ExceptionType));
        Assert.False(HasTag(exceptionEvent.Tags, "exception.message"));
        Assert.False(HasTag(exceptionEvent.Tags, "exception.stacktrace"));
        var started = GetWorkflowMeasurement(meterCapture, "elsa.workflow.started", context);
        var faulted = GetWorkflowMeasurement(meterCapture, "elsa.workflow.faulted", context);
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

        var span = GetStoppedActivity(activityCapture, "workflow.execute", WorkflowInstrumentation.WorkflowInstanceId, context.Id);
        Assert.Equal(ActivityStatusCode.Ok, span.Status);
        Assert.Equal(WorkflowSubStatus.Cancelled.ToString(), GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowSubStatus));
        Assert.Equal(false, GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowFaulted));
        Assert.DoesNotContain(meterCapture.LongMeasurements, x => IsWorkflowMeasurement(x, "elsa.workflow.faulted", context));
    }

    [Fact]
    public async Task WorkflowRunner_Should_Not_Start_When_WorkflowExecuting_Handler_Changes_SubStatus()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var activityExecutionContext = await new ActivityTestFixture(new TestActivity()).BuildAsync();
        var context = activityExecutionContext.WorkflowExecutionContext;
        var notificationSender = Substitute.For<INotificationSender>();
        notificationSender
            .SendAsync(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                if (callInfo.Arg<INotification>() is WorkflowExecuting)
                    context.Cancel();

                return Task.CompletedTask;
            });
        var runner = CreateWorkflowRunner(context, new NoopWorkflowExecutionPipeline(), notificationSender);

        await runner.RunAsync(context);

        Assert.Equal(WorkflowSubStatus.Cancelled, context.SubStatus);
        Assert.DoesNotContain(meterCapture.LongMeasurements, x => IsWorkflowMeasurement(x, "elsa.workflow.started", context));
        await notificationSender
            .DidNotReceive()
            .SendAsync(Arg.Is<INotification>(notification => notification is WorkflowStarted), Arg.Any<CancellationToken>());
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
        var span = GetStoppedActivity(activityCapture, "activity.execute", WorkflowInstrumentation.ActivityExecutionId, context.Id);
        Assert.Equal(ActivityStatus.Faulted.ToString(), GetTag(span.TagObjects, WorkflowInstrumentation.ActivityStatus));
        var activityDuration = GetActivityDuration(meterCapture, context);
        Assert.Equal(true, activityDuration.Tags[WorkflowInstrumentation.ActivityFaulted]);
        Assert.Equal(ActivityStatus.Faulted.ToString(), activityDuration.Tags[WorkflowInstrumentation.ActivityStatus]);
    }

    [Fact]
    public async Task ActivityInvoker_Should_Not_Emit_ExceptionType_When_Faulted_Without_Exception()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var context = await new ActivityTestFixture(new TestActivity()).BuildAsync();
        var invoker = new ActivityInvoker(new FaultingActivityExecutionPipeline(), new ActivityLoggerStateGenerator(), NullLogger<ActivityInvoker>.Instance);

        await invoker.InvokeAsync(context);

        var span = GetStoppedActivity(activityCapture, "activity.execute", WorkflowInstrumentation.ActivityExecutionId, context.Id);
        Assert.Equal(ActivityStatusCode.Error, span.Status);
        Assert.Equal(ActivityStatus.Faulted.ToString(), GetTag(span.TagObjects, WorkflowInstrumentation.ActivityStatus));
        Assert.False(HasTag(span.TagObjects, WorkflowInstrumentation.ExceptionType));
    }

    [Fact]
    public async Task WorkflowRunner_Should_Use_Context_Exception_When_Faulted_Without_Thrown_Exception()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var activityExecutionContext = await new ActivityTestFixture(new TestActivity()).BuildAsync();
        var context = activityExecutionContext.WorkflowExecutionContext;
        var exception = new InvalidOperationException("Faulted by middleware");
        var runner = CreateWorkflowRunner(context, new FaultingWorkflowExecutionPipeline(exception));

        await runner.RunAsync(context);

        var span = GetStoppedActivity(activityCapture, "workflow.execute", WorkflowInstrumentation.WorkflowInstanceId, context.Id);
        Assert.Equal(ActivityStatusCode.Error, span.Status);
        Assert.Equal(typeof(InvalidOperationException).FullName, GetTag(span.TagObjects, WorkflowInstrumentation.ExceptionType));
        Assert.Equal(exception, context.Exception);
        _ = GetWorkflowMeasurement(meterCapture, "elsa.workflow.faulted", context);
    }

    private static WorkflowRunner CreateWorkflowRunner(WorkflowExecutionContext context, IWorkflowExecutionPipeline pipeline, INotificationSender? notificationSender = null)
    {
        if (notificationSender == null)
        {
            notificationSender = Substitute.For<INotificationSender>();
            notificationSender
                .SendAsync(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
        }

        var workflowStateExtractor = Substitute.For<IWorkflowStateExtractor>();
        workflowStateExtractor.Extract(context).Returns(_ => new WorkflowState
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

    private static DiagnosticsActivity GetStoppedActivity(ActivityCapture capture, string operationName, string tagKey, object? tagValue)
    {
        return Assert.Single(capture.StoppedActivities, activity =>
            activity.OperationName == operationName &&
            activity.TagObjects.Any(tag => tag.Key == tagKey && Equals(tag.Value, tagValue)));
    }

    private static CapturedMeasurement<double> GetActivityDuration(MeterCapture capture, ActivityExecutionContext context)
    {
        return Assert.Single(capture.DoubleMeasurements, measurement =>
            measurement.InstrumentName == "elsa.activity.duration" &&
            measurement.Value >= 0 &&
            HasTag(measurement.Tags, WorkflowInstrumentation.WorkflowDefinitionId, context.WorkflowExecutionContext.Workflow.Identity.DefinitionId) &&
            HasTag(measurement.Tags, WorkflowInstrumentation.ActivityType, context.Activity.Type));
    }

    private static CapturedMeasurement<long> GetWorkflowMeasurement(MeterCapture capture, string instrumentName, WorkflowExecutionContext context)
    {
        return Assert.Single(capture.LongMeasurements, measurement => IsWorkflowMeasurement(measurement, instrumentName, context));
    }

    private static bool IsWorkflowMeasurement(CapturedMeasurement<long> measurement, string instrumentName, WorkflowExecutionContext context)
    {
        return measurement.InstrumentName == instrumentName &&
               measurement.Value == 1 &&
               HasTag(measurement.Tags, WorkflowInstrumentation.WorkflowDefinitionId, context.Workflow.Identity.DefinitionId);
    }

    private static bool HasTag(IReadOnlyDictionary<string, object?> tags, string key, object? value)
    {
        return tags.TryGetValue(key, out var tagValue) && Equals(tagValue, value);
    }

    private static object? GetTag(IEnumerable<KeyValuePair<string, object?>> tags, string key)
    {
        object? value = null;
        var found = false;

        foreach (var tag in tags)
        {
            if (tag.Key != key)
                continue;

            value = tag.Value;
            found = true;
        }

        Assert.True(found, $"Expected tag '{key}' to be present.");
        return value;
    }

    private static bool HasTag(IEnumerable<KeyValuePair<string, object?>> tags, string key)
    {
        return tags.Any(tag => tag.Key == key);
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

    private sealed class FaultingWorkflowExecutionPipeline(Exception exception) : IWorkflowExecutionPipeline
    {
        public Action<IWorkflowExecutionPipelineBuilder> ConfigurePipelineBuilder => _ => { };
        public WorkflowMiddlewareDelegate Pipeline => _ => ValueTask.CompletedTask;

        public WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionPipelineBuilder> setup) => Pipeline;

        public async Task ExecuteAsync(WorkflowExecutionContext context)
        {
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw exception,
                context.SystemClock,
                NullLogger<ExceptionHandlingMiddleware>.Instance);

            await middleware.InvokeAsync(context);
        }
    }

    private sealed class NoopWorkflowExecutionPipeline : IWorkflowExecutionPipeline
    {
        public Action<IWorkflowExecutionPipelineBuilder> ConfigurePipelineBuilder => _ => { };
        public WorkflowMiddlewareDelegate Pipeline => _ => ValueTask.CompletedTask;

        public WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionPipelineBuilder> setup) => Pipeline;

        public Task ExecuteAsync(WorkflowExecutionContext context) => Task.CompletedTask;
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

        public ConcurrentQueue<CapturedMeasurement<long>> LongMeasurements { get; } = new();
        public ConcurrentQueue<CapturedMeasurement<double>> DoubleMeasurements { get; } = new();

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

    private readonly record struct CapturedMeasurement<T>(string InstrumentName, T Value, IReadOnlyDictionary<string, object?> Tags);
}

[CollectionDefinition(nameof(WorkflowInstrumentationTestCollection), DisableParallelization = true)]
public sealed class WorkflowInstrumentationTestCollection
{
}
