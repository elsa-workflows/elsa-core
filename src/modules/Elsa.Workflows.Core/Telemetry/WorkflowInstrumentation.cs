using System.Diagnostics;
using System.Diagnostics.Metrics;

using DiagnosticsActivity = System.Diagnostics.Activity;
using DiagnosticsActivityKind = System.Diagnostics.ActivityKind;

namespace Elsa.Workflows.Telemetry;

/// <summary>
/// Provides tracing and metrics instrumentation for workflow and activity execution.
/// </summary>
public static class WorkflowInstrumentation
{
    public const string ActivitySourceName = "Elsa.Workflows";
    public const string MeterName = "Elsa.Workflows";

    public const string WorkflowSystem = "workflow.system";
    public const string WorkflowOperationName = "workflow.operation.name";
    public const string WorkflowName = "workflow.name";
    public const string WorkflowInstanceId = "workflow.instance.id";
    public const string WorkflowDefinitionId = "workflow.definition.id";
    public const string WorkflowDefinitionVersion = "workflow.definition.version";
    public const string WorkflowDefinitionVersionId = "workflow.definition.version.id";
    public const string WorkflowStatus = "workflow.status";
    public const string WorkflowSubStatus = "workflow.substatus";
    public const string WorkflowFaulted = "workflow.faulted";
    public const string WorkflowParentInstanceId = "workflow.parent_instance.id";
    public const string WorkflowCorrelationId = "workflow.correlation.id";

    public const string ActivityOperationName = "workflow.activity.operation.name";
    public const string ActivityId = "workflow.activity.id";
    public const string ActivityName = "workflow.activity.name";
    public const string ActivityType = "workflow.activity.type";
    public const string ActivityVersion = "workflow.activity.version";
    public const string ActivityExecutionId = "workflow.activity.execution.id";
    public const string ActivityStatus = "workflow.activity.status";
    public const string ActivityOutcome = "workflow.activity.outcome";
    public const string ActivityParentExecutionId = "workflow.activity.parent_execution.id";
    public const string ActivityScheduledByExecutionId = "workflow.activity.scheduled_by_execution.id";
    public const string ActivityFaulted = "workflow.activity.faulted";

    public const string TenantId = "elsa.tenant.id";
    public const string ErrorType = "error.type";

    private const string SystemName = "elsa";
    private const string WorkflowExecuteOperation = "workflow.execute";
    private const string ActivityExecuteOperation = "activity.execute";

    private static readonly string? Version = typeof(WorkflowInstrumentation).Assembly.GetName().Version?.ToString();
    private static readonly ActivitySource Source = new(ActivitySourceName, Version);
    private static readonly Meter Meter = new(MeterName, Version);
    private static readonly Counter<long> WorkflowStartedCounter = Meter.CreateCounter<long>("elsa.workflow.started", description: "Number of workflow instances started.");
    private static readonly Counter<long> WorkflowCompletedCounter = Meter.CreateCounter<long>("elsa.workflow.completed", description: "Number of workflow instances completed.");
    private static readonly Counter<long> WorkflowFaultedCounter = Meter.CreateCounter<long>("elsa.workflow.faulted", description: "Number of workflow execution cycles that faulted.");
    private static readonly Histogram<double> ActivityDuration = Meter.CreateHistogram<double>("elsa.activity.duration", "s", "Duration of activity execution.");

    internal static WorkflowInstrumentationScope StartWorkflow(WorkflowExecutionContext context, bool? isStarting = null)
    {
        var shouldRecordStarted = isStarting ?? context.SubStatus == Workflows.WorkflowSubStatus.Pending;
        var activity = Source.StartActivity(WorkflowExecuteOperation, DiagnosticsActivityKind.Internal);

        if (activity != null)
        {
            SetWorkflowTags(activity, context);
            activity.SetTag(WorkflowOperationName, WorkflowExecuteOperation);
        }

        if (shouldRecordStarted)
            WorkflowStartedCounter.Add(1, CreateWorkflowTags(context, false));

        return new WorkflowInstrumentationScope(activity);
    }

    internal static ActivityInstrumentationScope StartActivity(ActivityExecutionContext context)
    {
        var activity = Source.StartActivity(ActivityExecuteOperation, DiagnosticsActivityKind.Internal);

        if (activity != null)
        {
            SetWorkflowTags(activity, context.WorkflowExecutionContext);
            SetActivityTags(activity, context);
            activity.SetTag(ActivityOperationName, ActivityExecuteOperation);
        }

        return new ActivityInstrumentationScope(activity, Stopwatch.GetTimestamp());
    }

    internal static void StopWorkflow(WorkflowInstrumentationScope scope, WorkflowExecutionContext context, Exception? exception)
    {
        var activity = scope.Activity;
        var cancelled = exception is OperationCanceledException || context.SubStatus == Workflows.WorkflowSubStatus.Cancelled;
        var faulted = !cancelled && (exception != null || context.SubStatus == Workflows.WorkflowSubStatus.Faulted);

        if (activity != null)
        {
            SetWorkflowTags(activity, context);
            activity.SetTag(WorkflowFaulted, faulted);
            SetError(activity, exception, faulted);
            activity.Dispose();
        }

        if (faulted)
            WorkflowFaultedCounter.Add(1, CreateWorkflowTags(context));
        else if (context.SubStatus == Workflows.WorkflowSubStatus.Finished)
            WorkflowCompletedCounter.Add(1, CreateWorkflowTags(context));
    }

    internal static void StopActivity(ActivityInstrumentationScope scope, ActivityExecutionContext context, Exception? exception)
    {
        var activity = scope.Activity;
        var cancelled = exception is OperationCanceledException || context.Status == Workflows.ActivityStatus.Canceled;
        var faulted = !cancelled && (exception != null || context.Status == Workflows.ActivityStatus.Faulted);
        var duration = Stopwatch.GetElapsedTime(scope.StartTimestamp).TotalSeconds;

        ActivityDuration.Record(duration, CreateActivityTags(context, faulted));

        if (activity != null)
        {
            SetActivityTags(activity, context);
            activity.SetTag(ActivityFaulted, faulted);
            SetActivityOutcome(activity, context);
            SetError(activity, exception ?? context.Exception, faulted);
            activity.Dispose();
        }
    }

    private static void SetWorkflowTags(DiagnosticsActivity activity, WorkflowExecutionContext context)
    {
        var workflow = context.Workflow;
        var identity = workflow.Identity;

        activity.SetTag(WorkflowSystem, SystemName);
        activity.SetTag(WorkflowName, workflow.WorkflowMetadata.Name ?? workflow.Name);
        activity.SetTag(WorkflowInstanceId, context.Id);
        activity.SetTag(WorkflowDefinitionId, identity.DefinitionId);
        activity.SetTag(WorkflowDefinitionVersion, identity.Version);
        activity.SetTag(WorkflowDefinitionVersionId, identity.Id);
        activity.SetTag(WorkflowStatus, context.Status.ToString());
        activity.SetTag(WorkflowSubStatus, context.SubStatus.ToString());
        activity.SetTag(WorkflowParentInstanceId, context.ParentWorkflowInstanceId);
        activity.SetTag(WorkflowCorrelationId, context.CorrelationId);
        activity.SetTag(TenantId, identity.TenantId);
    }

    private static void SetActivityTags(DiagnosticsActivity activity, ActivityExecutionContext context)
    {
        var currentActivity = context.Activity;

        activity.SetTag(ActivityId, currentActivity.Id);
        activity.SetTag(ActivityName, currentActivity.Name ?? context.ActivityDescriptor.DisplayName ?? context.ActivityDescriptor.Name);
        activity.SetTag(ActivityType, currentActivity.Type);
        activity.SetTag(ActivityVersion, currentActivity.Version);
        activity.SetTag(ActivityExecutionId, context.Id);
        activity.SetTag(ActivityStatus, context.Status.ToString());
        activity.SetTag(ActivityParentExecutionId, context.ParentActivityExecutionContext?.Id);
        activity.SetTag(ActivityScheduledByExecutionId, context.SchedulingActivityExecutionId);
    }

    private static void SetActivityOutcome(DiagnosticsActivity activity, ActivityExecutionContext context)
    {
        if (!context.JournalData.TryGetValue("Outcomes", out var outcomes))
            return;

        var outcome = outcomes switch
        {
            IEnumerable<string> names => string.Join(",", names),
            _ => outcomes?.ToString()
        };

        activity.SetTag(ActivityOutcome, outcome);
    }

    private static void SetError(DiagnosticsActivity activity, Exception? exception, bool faulted)
    {
        if (!faulted)
        {
            activity.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        activity.SetStatus(ActivityStatusCode.Error, "Faulted");

        if (exception != null)
            activity.SetTag(ErrorType, exception.GetType().FullName);
    }

    private static TagList CreateWorkflowTags(WorkflowExecutionContext context, bool includeExecutionStatus = true)
    {
        var workflow = context.Workflow;
        var identity = workflow.Identity;
        var tags = new TagList
        {
            { WorkflowSystem, SystemName },
            { WorkflowDefinitionId, identity.DefinitionId },
            { WorkflowDefinitionVersion, identity.Version }
        };

        if (includeExecutionStatus)
        {
            tags.Add(WorkflowStatus, context.Status.ToString());
            tags.Add(WorkflowSubStatus, context.SubStatus.ToString());
        }

        AddIfNotNull(ref tags, WorkflowName, workflow.WorkflowMetadata.Name ?? workflow.Name);
        AddIfNotNull(ref tags, TenantId, identity.TenantId);
        return tags;
    }

    private static TagList CreateActivityTags(ActivityExecutionContext context, bool faulted)
    {
        var currentActivity = context.Activity;
        var tags = new TagList
        {
            { WorkflowSystem, SystemName },
            { WorkflowDefinitionId, context.WorkflowExecutionContext.Workflow.Identity.DefinitionId },
            { ActivityType, currentActivity.Type },
            { ActivityVersion, currentActivity.Version },
            { ActivityStatus, faulted ? Workflows.ActivityStatus.Faulted.ToString() : context.Status.ToString() },
            { ActivityFaulted, faulted }
        };

        AddIfNotNull(ref tags, TenantId, context.WorkflowExecutionContext.Workflow.Identity.TenantId);
        return tags;
    }

    private static void AddIfNotNull(ref TagList tags, string key, object? value)
    {
        if (value != null)
            tags.Add(key, value);
    }
}

internal readonly record struct WorkflowInstrumentationScope(DiagnosticsActivity? Activity);

internal readonly record struct ActivityInstrumentationScope(DiagnosticsActivity? Activity, long StartTimestamp);
