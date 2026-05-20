using System.Diagnostics;
using System.Diagnostics.Metrics;
using Elsa.Common;
using Elsa.Mediator.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.CommitStates;
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
        Assert.Equal(ActivityStatus.Completed.ToString(), tags[WorkflowInstrumentation.ActivityStatus]);
        var activityDuration = Assert.Single(meterCapture.DoubleMeasurements, x => x.InstrumentName == "elsa.activity.duration" && x.Value >= 0);
        Assert.Equal("Test activity", activityDuration.Tags[WorkflowInstrumentation.ActivityName]);
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
        Assert.Equal("Test workflow", started.Tags[WorkflowInstrumentation.WorkflowName]);
        Assert.Equal("Test workflow", faulted.Tags[WorkflowInstrumentation.WorkflowName]);
    }

    [Fact]
    public async Task ActivityInvoker_Should_Not_Replace_Pipeline_Exception_When_Outcome_Is_Null()
    {
        using var activityCapture = new ActivityCapture();
        var context = await new ActivityTestFixture(new TestActivity()).BuildAsync();
        var invoker = new ActivityInvoker(new ThrowingActivityExecutionPipeline(), new ActivityLoggerStateGenerator(), NullLogger<ActivityInvoker>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => invoker.InvokeAsync(context));

        Assert.Equal("Pipeline failed", exception.Message);
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

    private sealed class ThrowingWorkflowExecutionPipeline : IWorkflowExecutionPipeline
    {
        public Action<IWorkflowExecutionPipelineBuilder> ConfigurePipelineBuilder => _ => { };
        public WorkflowMiddlewareDelegate Pipeline => _ => ValueTask.CompletedTask;

        public WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionPipelineBuilder> setup) => Pipeline;

        public Task ExecuteAsync(WorkflowExecutionContext context) => throw new InvalidOperationException("Pipeline failed");
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
                ActivityStopped = activity => StoppedActivities.Add(activity)
            };

            ActivitySource.AddActivityListener(_listener);
        }

        public IList<DiagnosticsActivity> StoppedActivities { get; } = new List<DiagnosticsActivity>();

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

            _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) => LongMeasurements.Add(new(instrument.Name, measurement, CaptureTags(tags))));
            _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) => DoubleMeasurements.Add(new(instrument.Name, measurement, CaptureTags(tags))));
            _listener.Start();
        }

        public IList<Measurement<long>> LongMeasurements { get; } = new List<Measurement<long>>();
        public IList<Measurement<double>> DoubleMeasurements { get; } = new List<Measurement<double>>();

        public void Dispose() => _listener.Dispose();

        private static IReadOnlyDictionary<string, object?> CaptureTags(ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            var result = new Dictionary<string, object?>();

            foreach (var tag in tags)
                result[tag.Key] = tag.Value;

            return result;
        }
    }

    private readonly record struct Measurement<T>(string InstrumentName, T Value, IReadOnlyDictionary<string, object?> Tags);
}

[CollectionDefinition(nameof(WorkflowInstrumentationTestCollection), DisableParallelization = true)]
public sealed class WorkflowInstrumentationTestCollection
{
}
