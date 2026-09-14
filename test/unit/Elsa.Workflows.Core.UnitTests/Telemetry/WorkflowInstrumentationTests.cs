using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.Telemetry;

using DiagnosticsActivity = System.Diagnostics.Activity;

public class WorkflowInstrumentationTests
{
    [Test]
    public async Task ActivityInvoker_Should_Emit_Activity_Span_And_Duration_Metric()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var activity = new TestActivity { Name = "Test activity" };
        var context = await BuildTelemetryContextAsync(activity);
        var invoker = new ActivityInvoker(new CompletingActivityExecutionPipeline(), new ActivityLoggerStateGenerator(), NullLogger<ActivityInvoker>.Instance);

        await invoker.InvokeAsync(context);

        var span = await GetStoppedActivity(activityCapture, "activity.execute", WorkflowInstrumentation.ActivityExecutionId, context.Id);
        await Assert.That(span.OperationName).IsEqualTo("activity.execute");
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.ActivityType)).IsEqualTo(activity.Type);
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowDefinitionVersionId)).IsEqualTo(context.WorkflowExecutionContext.Workflow.Identity.Id);
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.ActivityStatus)).IsEqualTo(ActivityStatus.Completed.ToString());
        var activityDuration = await GetActivityDuration(meterCapture, context);
        await Assert.That(activityDuration.Tags.ContainsKey(WorkflowInstrumentation.WorkflowDefinitionVersionId)).IsFalse();
        await Assert.That(activityDuration.Tags.ContainsKey(WorkflowInstrumentation.ActivityName)).IsFalse();
        await Assert.That((bool)activityDuration.Tags[WorkflowInstrumentation.ActivityFaulted]!).IsFalse();
    }

    [Test]
    public async Task ActivityInvoker_Should_Not_Record_Faulted_Metric_When_Pipeline_Cancels()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var context = await BuildTelemetryContextAsync(new TestActivity());
        var invoker = new ActivityInvoker(new CancellingActivityExecutionPipeline(), new ActivityLoggerStateGenerator(), NullLogger<ActivityInvoker>.Instance);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => invoker.InvokeAsync(context));

        var span = await GetStoppedActivity(activityCapture, "activity.execute", WorkflowInstrumentation.ActivityExecutionId, context.Id);
        await Assert.That(span.Status).IsEqualTo(ActivityStatusCode.Ok);
        var activityDuration = await GetActivityDuration(meterCapture, context);
        await Assert.That((bool)activityDuration.Tags[WorkflowInstrumentation.ActivityFaulted]!).IsFalse();
        await Assert.That(activityDuration.Tags[WorkflowInstrumentation.ActivityStatus]).IsEqualTo(ActivityStatus.Canceled.ToString());
    }

    [Test]
    public async Task ActivityInvoker_Should_Record_Canceled_Status_When_Pipeline_Cancels_Before_Status_Transition()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var context = await BuildTelemetryContextAsync(new TestActivity());
        var invoker = new ActivityInvoker(new NonMutatingCancellingActivityExecutionPipeline(), new ActivityLoggerStateGenerator(), NullLogger<ActivityInvoker>.Instance);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => invoker.InvokeAsync(context));

        var span = await GetStoppedActivity(activityCapture, "activity.execute", WorkflowInstrumentation.ActivityExecutionId, context.Id);
        await Assert.That(span.Status).IsEqualTo(ActivityStatusCode.Ok);
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.ActivityStatus)).IsEqualTo(ActivityStatus.Canceled.ToString());
        await Assert.That((bool)GetTag(span.TagObjects, WorkflowInstrumentation.ActivityFaulted)!).IsFalse();
        var activityDuration = await GetActivityDuration(meterCapture, context);
        await Assert.That(activityDuration.Tags[WorkflowInstrumentation.ActivityStatus]).IsEqualTo(ActivityStatus.Canceled.ToString());
        await Assert.That((bool)activityDuration.Tags[WorkflowInstrumentation.ActivityFaulted]!).IsFalse();
    }

    [Test]
    public async Task WorkflowRunner_Should_Record_Completed_Metric_When_Pipeline_Finishes()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var activityExecutionContext = await BuildTelemetryContextAsync(new TestActivity());
        var context = activityExecutionContext.WorkflowExecutionContext;
        context.Workflow.WorkflowMetadata = new("Test workflow");
        context.ParentWorkflowInstanceId = "parent-instance-id";
        context.CorrelationId = "";
        context.Workflow.Identity = context.Workflow.Identity with { TenantId = " " };
        var runner = CreateWorkflowRunner(context, new CompletingWorkflowExecutionPipeline());

        await runner.RunAsync(context);

        var span = await GetStoppedActivity(activityCapture, "workflow.execute", WorkflowInstrumentation.WorkflowInstanceId, context.Id);
        await Assert.That(span.Status).IsEqualTo(ActivityStatusCode.Ok);
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowDefinitionVersionId)).IsEqualTo(context.Workflow.Identity.Id);
        var workflowSubStatus = GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowSubStatus);
        await Assert.That(workflowSubStatus).IsEqualTo(WorkflowSubStatus.Finished.ToString());
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowParentInstanceId)).IsEqualTo("parent-instance-id");
        await Assert.That(HasTag(span.TagObjects, WorkflowInstrumentation.WorkflowCorrelationId)).IsFalse();
        await Assert.That(HasTag(span.TagObjects, WorkflowInstrumentation.TenantId)).IsFalse();
        await Assert.That(HasTag(span.TagObjects, "workflow.parent_instance.id")).IsFalse();
        var started = await GetWorkflowMeasurement(meterCapture, "elsa.workflow.started", context);
        var completed = await GetWorkflowMeasurement(meterCapture, "elsa.workflow.completed", context);
        await Assert.That(started.Tags.ContainsKey(WorkflowInstrumentation.WorkflowDefinitionVersionId)).IsFalse();
        await Assert.That(started.Tags.ContainsKey(WorkflowInstrumentation.TenantId)).IsFalse();
        await Assert.That(completed.Tags.ContainsKey(WorkflowInstrumentation.WorkflowDefinitionVersionId)).IsFalse();
        await Assert.That(started.Tags.ContainsKey(WorkflowInstrumentation.WorkflowSubStatus)).IsFalse();
        await Assert.That(completed.Tags[WorkflowInstrumentation.WorkflowSubStatus]).IsEqualTo(WorkflowSubStatus.Finished.ToString());
    }

    [Test]
    public async Task WorkflowRunner_Should_Record_Faulted_Span_And_Metric_When_Pipeline_Throws()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var activityExecutionContext = await BuildTelemetryContextAsync(new TestActivity());
        var context = activityExecutionContext.WorkflowExecutionContext;
        context.Workflow.WorkflowMetadata = new("Test workflow");
        var runner = CreateWorkflowRunner(context, new ThrowingWorkflowExecutionPipeline());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => runner.RunAsync(context));

        var span = await GetStoppedActivity(activityCapture, "workflow.execute", WorkflowInstrumentation.WorkflowInstanceId, context.Id);
        await Assert.That(span.OperationName).IsEqualTo("workflow.execute");
        await Assert.That(span.Status).IsEqualTo(ActivityStatusCode.Error);
        await Assert.That(context.SubStatus).IsEqualTo(WorkflowSubStatus.Executing);
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowInstanceId)).IsEqualTo(context.Id);
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowStatus)).IsEqualTo(WorkflowStatus.Finished.ToString());
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowSubStatus)).IsEqualTo(WorkflowSubStatus.Faulted.ToString());
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.ExceptionType)).IsEqualTo(typeof(InvalidOperationException).FullName);
        await Assert.That(HasTag(span.TagObjects, "exception.message")).IsFalse();
        await Assert.That(HasTag(span.TagObjects, "exception.stacktrace")).IsFalse();
        var exceptionEvent = await Assert.That(span.Events).HasSingleItem(x => x.Name == "exception");
        await Assert.That(GetTag(exceptionEvent.Tags, WorkflowInstrumentation.ExceptionType)).IsEqualTo(typeof(InvalidOperationException).FullName);
        await Assert.That(HasTag(exceptionEvent.Tags, "exception.message")).IsFalse();
        await Assert.That(HasTag(exceptionEvent.Tags, "exception.stacktrace")).IsFalse();
        var started = await GetWorkflowMeasurement(meterCapture, "elsa.workflow.started", context);
        var faulted = await GetWorkflowMeasurement(meterCapture, "elsa.workflow.faulted", context);
        await Assert.That(started.Tags.ContainsKey(WorkflowInstrumentation.WorkflowDefinitionVersionId)).IsFalse();
        await Assert.That(faulted.Tags.ContainsKey(WorkflowInstrumentation.WorkflowDefinitionVersionId)).IsFalse();
        await Assert.That(started.Tags[WorkflowInstrumentation.WorkflowName]).IsEqualTo("Test workflow");
        await Assert.That(faulted.Tags[WorkflowInstrumentation.WorkflowName]).IsEqualTo("Test workflow");
        await Assert.That(faulted.Tags[WorkflowInstrumentation.WorkflowStatus]).IsEqualTo(WorkflowStatus.Finished.ToString());
        await Assert.That(faulted.Tags[WorkflowInstrumentation.WorkflowSubStatus]).IsEqualTo(WorkflowSubStatus.Faulted.ToString());
    }

    [Test]
    public async Task WorkflowRunner_Should_Not_Record_Faulted_Span_Or_Metric_When_Pipeline_Cancels()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var activityExecutionContext = await BuildTelemetryContextAsync(new TestActivity());
        var context = activityExecutionContext.WorkflowExecutionContext;
        var runner = CreateWorkflowRunner(context, new CancellingWorkflowExecutionPipeline());

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => runner.RunAsync(context));

        var span = await GetStoppedActivity(activityCapture, "workflow.execute", WorkflowInstrumentation.WorkflowInstanceId, context.Id);
        await Assert.That(span.Status).IsEqualTo(ActivityStatusCode.Ok);
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowSubStatus)).IsEqualTo(WorkflowSubStatus.Cancelled.ToString());
        await Assert.That((bool)GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowFaulted)!).IsFalse();
        await Assert.That(meterCapture.LongMeasurements).DoesNotContain(x => IsWorkflowMeasurement(x, "elsa.workflow.faulted", context));
    }

    [Test]
    public async Task WorkflowRunner_Should_Record_Cancelled_SubStatus_When_Pipeline_Cancels_Before_Status_Transition()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var activityExecutionContext = await BuildTelemetryContextAsync(new TestActivity());
        var context = activityExecutionContext.WorkflowExecutionContext;
        var runner = CreateWorkflowRunner(context, new NonMutatingCancellingWorkflowExecutionPipeline());

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => runner.RunAsync(context));

        var span = await GetStoppedActivity(activityCapture, "workflow.execute", WorkflowInstrumentation.WorkflowInstanceId, context.Id);
        await Assert.That(span.Status).IsEqualTo(ActivityStatusCode.Ok);
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowStatus)).IsEqualTo(WorkflowStatus.Finished.ToString());
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowSubStatus)).IsEqualTo(WorkflowSubStatus.Cancelled.ToString());
        await Assert.That((bool)GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowFaulted)!).IsFalse();
        await Assert.That(meterCapture.LongMeasurements).DoesNotContain(x => IsWorkflowMeasurement(x, "elsa.workflow.faulted", context));
    }

    [Test]
    public async Task WorkflowRunner_Should_Record_Cancel_When_ExceptionHandlingMiddleware_Catches_Cancellation()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var activityExecutionContext = await BuildTelemetryContextAsync(new TestActivity());
        var context = activityExecutionContext.WorkflowExecutionContext;
        var runner = CreateWorkflowRunner(context, new ExceptionHandlingCancellingWorkflowExecutionPipeline());

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => runner.RunAsync(context));

        var span = await GetStoppedActivity(activityCapture, "workflow.execute", WorkflowInstrumentation.WorkflowInstanceId, context.Id);
        await Assert.That(span.Status).IsEqualTo(ActivityStatusCode.Ok);
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowSubStatus)).IsEqualTo(WorkflowSubStatus.Cancelled.ToString());
        await Assert.That((bool)GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowFaulted)!).IsFalse();
        await Assert.That(meterCapture.LongMeasurements).DoesNotContain(x => IsWorkflowMeasurement(x, "elsa.workflow.faulted", context));
    }

    [Test]
    public async Task WorkflowRunner_Should_Not_Start_When_WorkflowExecuting_Handler_Changes_SubStatus()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var activityExecutionContext = await BuildTelemetryContextAsync(new TestActivity());
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

        await Assert.That(context.SubStatus).IsEqualTo(WorkflowSubStatus.Cancelled);
        await Assert.That(meterCapture.LongMeasurements).DoesNotContain(x => IsWorkflowMeasurement(x, "elsa.workflow.started", context));
        await notificationSender
            .DidNotReceive()
            .SendAsync(Arg.Is<INotification>(notification => notification is WorkflowStarted), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ActivityInvoker_Should_Not_Replace_Pipeline_Exception_When_Outcome_Is_Null()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var context = await BuildTelemetryContextAsync(new TestActivity());
        var invoker = new ActivityInvoker(new ThrowingActivityExecutionPipeline(), new ActivityLoggerStateGenerator(), NullLogger<ActivityInvoker>.Instance);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => invoker.InvokeAsync(context));

        await Assert.That(exception!.Message).IsEqualTo("Pipeline failed");
        var span = await GetStoppedActivity(activityCapture, "activity.execute", WorkflowInstrumentation.ActivityExecutionId, context.Id);
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.ActivityStatus)).IsEqualTo(ActivityStatus.Faulted.ToString());
        var activityDuration = await GetActivityDuration(meterCapture, context);
        await Assert.That((bool)activityDuration.Tags[WorkflowInstrumentation.ActivityFaulted]!).IsTrue();
        await Assert.That(activityDuration.Tags[WorkflowInstrumentation.ActivityStatus]).IsEqualTo(ActivityStatus.Faulted.ToString());
    }

    [Test]
    public async Task ActivityInvoker_Should_Record_Fault_When_Cancelled_Context_Throws_NonCancellation_Exception()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var context = await BuildTelemetryContextAsync(new TestActivity());
        var invoker = new ActivityInvoker(new CancelledThenThrowingActivityExecutionPipeline(), new ActivityLoggerStateGenerator(), NullLogger<ActivityInvoker>.Instance);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => invoker.InvokeAsync(context));

        var span = await GetStoppedActivity(activityCapture, "activity.execute", WorkflowInstrumentation.ActivityExecutionId, context.Id);
        await Assert.That(span.Status).IsEqualTo(ActivityStatusCode.Error);
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.ActivityStatus)).IsEqualTo(ActivityStatus.Faulted.ToString());
        await Assert.That((bool)GetTag(span.TagObjects, WorkflowInstrumentation.ActivityFaulted)!).IsTrue();
        var activityDuration = await GetActivityDuration(meterCapture, context);
        await Assert.That(activityDuration.Tags[WorkflowInstrumentation.ActivityStatus]).IsEqualTo(ActivityStatus.Faulted.ToString());
        await Assert.That((bool)activityDuration.Tags[WorkflowInstrumentation.ActivityFaulted]!).IsTrue();
    }

    [Test]
    public async Task ActivityInvoker_Should_Not_Emit_ExceptionType_When_Faulted_Without_Exception()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var context = await BuildTelemetryContextAsync(new TestActivity());
        var invoker = new ActivityInvoker(new FaultingActivityExecutionPipeline(), new ActivityLoggerStateGenerator(), NullLogger<ActivityInvoker>.Instance);

        await invoker.InvokeAsync(context);

        var span = await GetStoppedActivity(activityCapture, "activity.execute", WorkflowInstrumentation.ActivityExecutionId, context.Id);
        await Assert.That(span.Status).IsEqualTo(ActivityStatusCode.Error);
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.ActivityStatus)).IsEqualTo(ActivityStatus.Faulted.ToString());
        await Assert.That(HasTag(span.TagObjects, WorkflowInstrumentation.ExceptionType)).IsFalse();
    }

    [Test]
    public async Task WorkflowRunner_Should_Use_Context_Exception_When_Faulted_Without_Thrown_Exception()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var activityExecutionContext = await BuildTelemetryContextAsync(new TestActivity());
        var context = activityExecutionContext.WorkflowExecutionContext;
        var exception = new InvalidOperationException("Faulted by middleware");
        var runner = CreateWorkflowRunner(context, new FaultingWorkflowExecutionPipeline(exception));

        await runner.RunAsync(context);

        var span = await GetStoppedActivity(activityCapture, "workflow.execute", WorkflowInstrumentation.WorkflowInstanceId, context.Id);
        await Assert.That(span.Status).IsEqualTo(ActivityStatusCode.Error);
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.ExceptionType)).IsEqualTo(typeof(InvalidOperationException).FullName);
        _ = await GetWorkflowMeasurement(meterCapture, "elsa.workflow.faulted", context);
    }

    [Test]
    public async Task WorkflowRunner_Should_Record_Fault_When_Cancelled_Context_Throws_NonCancellation_Exception()
    {
        using var activityCapture = new ActivityCapture();
        using var meterCapture = new MeterCapture();
        var activityExecutionContext = await BuildTelemetryContextAsync(new TestActivity());
        var context = activityExecutionContext.WorkflowExecutionContext;
        var runner = CreateWorkflowRunner(context, new CancelledThenThrowingWorkflowExecutionPipeline());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => runner.RunAsync(context));

        var span = await GetStoppedActivity(activityCapture, "workflow.execute", WorkflowInstrumentation.WorkflowInstanceId, context.Id);
        await Assert.That(span.Status).IsEqualTo(ActivityStatusCode.Error);
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowStatus)).IsEqualTo(WorkflowStatus.Finished.ToString());
        await Assert.That(GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowSubStatus)).IsEqualTo(WorkflowSubStatus.Faulted.ToString());
        await Assert.That((bool)GetTag(span.TagObjects, WorkflowInstrumentation.WorkflowFaulted)!).IsTrue();
        var faulted = await GetWorkflowMeasurement(meterCapture, "elsa.workflow.faulted", context);
        await Assert.That(faulted.Tags[WorkflowInstrumentation.WorkflowSubStatus]).IsEqualTo(WorkflowSubStatus.Faulted.ToString());
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

    private static async Task<ActivityExecutionContext> BuildTelemetryContextAsync(IActivity activity)
    {
        var fixture = new ActivityTestFixture(activity)
            .ConfigureServices(services => services.AddSingleton<IIdentityGenerator, GuidIdentityGenerator>());
        var context = await fixture.BuildAsync();
        var invocationId = Guid.NewGuid().ToString("N");
        var identity = context.WorkflowExecutionContext.Workflow.Identity;
        context.WorkflowExecutionContext.Workflow.Identity = identity with
        {
            DefinitionId = invocationId,
            Id = $"{invocationId}:{identity.Version}"
        };
        return context;
    }

    private static async Task<DiagnosticsActivity> GetStoppedActivity(ActivityCapture capture, string operationName, string tagKey, object? tagValue)
    {
        return await Assert.That(capture.StoppedActivities).HasSingleItem(activity =>
            activity.OperationName == operationName &&
            activity.TagObjects.Any(tag => tag.Key == tagKey && Equals(tag.Value, tagValue)));
    }

    private static async Task<CapturedMeasurement<double>> GetActivityDuration(MeterCapture capture, ActivityExecutionContext context)
    {
        return await Assert.That(capture.DoubleMeasurements).HasSingleItem(measurement =>
            measurement.InstrumentName == "elsa.activity.duration" &&
            measurement.Value >= 0 &&
            HasTag(measurement.Tags, WorkflowInstrumentation.WorkflowDefinitionId, context.WorkflowExecutionContext.Workflow.Identity.DefinitionId) &&
            HasTag(measurement.Tags, WorkflowInstrumentation.ActivityType, context.Activity.Type));
    }

    private static async Task<CapturedMeasurement<long>> GetWorkflowMeasurement(MeterCapture capture, string instrumentName, WorkflowExecutionContext context)
    {
        return await Assert.That(capture.LongMeasurements).HasSingleItem(measurement => IsWorkflowMeasurement(measurement, instrumentName, context));
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

        foreach (var tag in tags.Where(tag => tag.Key == key))
        {
            value = tag.Value;
            found = true;
        }

        if (!found)
            throw new InvalidOperationException($"Expected tag '{key}' to be present.");

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

    private sealed class NonMutatingCancellingActivityExecutionPipeline : IActivityExecutionPipeline
    {
        public ActivityMiddlewareDelegate Pipeline => _ => ValueTask.CompletedTask;

        public ActivityMiddlewareDelegate Setup(Action<IActivityExecutionPipelineBuilder> setup) => Pipeline;

        public Task ExecuteAsync(ActivityExecutionContext context) => throw new OperationCanceledException();
    }

    private sealed class CancelledThenThrowingActivityExecutionPipeline : IActivityExecutionPipeline
    {
        public ActivityMiddlewareDelegate Pipeline => _ => ValueTask.CompletedTask;

        public ActivityMiddlewareDelegate Setup(Action<IActivityExecutionPipelineBuilder> setup) => Pipeline;

        public Task ExecuteAsync(ActivityExecutionContext context)
        {
            context.TransitionTo(ActivityStatus.Canceled);
            throw new InvalidOperationException("Pipeline failed after cancellation.");
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

    private sealed class NonMutatingCancellingWorkflowExecutionPipeline : IWorkflowExecutionPipeline
    {
        public Action<IWorkflowExecutionPipelineBuilder> ConfigurePipelineBuilder => _ => { };
        public WorkflowMiddlewareDelegate Pipeline => _ => ValueTask.CompletedTask;

        public WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionPipelineBuilder> setup) => Pipeline;

        public Task ExecuteAsync(WorkflowExecutionContext context) => throw new OperationCanceledException();
    }

    private sealed class CancelledThenThrowingWorkflowExecutionPipeline : IWorkflowExecutionPipeline
    {
        public Action<IWorkflowExecutionPipelineBuilder> ConfigurePipelineBuilder => _ => { };
        public WorkflowMiddlewareDelegate Pipeline => _ => ValueTask.CompletedTask;

        public WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionPipelineBuilder> setup) => Pipeline;

        public Task ExecuteAsync(WorkflowExecutionContext context)
        {
            context.Cancel();
            throw new InvalidOperationException("Pipeline failed after cancellation.");
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

    private sealed class ExceptionHandlingCancellingWorkflowExecutionPipeline : IWorkflowExecutionPipeline
    {
        public Action<IWorkflowExecutionPipelineBuilder> ConfigurePipelineBuilder => _ => { };
        public WorkflowMiddlewareDelegate Pipeline => _ => ValueTask.CompletedTask;

        public WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionPipelineBuilder> setup) => Pipeline;

        public async Task ExecuteAsync(WorkflowExecutionContext context)
        {
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new OperationCanceledException(),
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
