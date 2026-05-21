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
    public const string WorkflowParentInstanceId = "workflow.parent.instance.id";
    public const string WorkflowCorrelationId = "workflow.correlation.id";

    public const string ActivityOperationName = "workflow.activity.operation.name";
    public const string ActivityId = "workflow.activity.id";
    public const string ActivityName = "workflow.activity.name";
    public const string ActivityType = "workflow.activity.type";
    public const string ActivityVersion = "workflow.activity.version";
    public const string ActivityExecutionId = "workflow.activity.execution.id";
    public const string ActivityStatus = "workflow.activity.status";
    public const string ActivityOutcome = "workflow.activity.outcome";
    public const string ActivityParentExecutionId = "workflow.activity.parent.execution.id";
    public const string ActivityScheduledByExecutionId = "workflow.activity.scheduled.by.execution.id";
    public const string ActivityFaulted = "workflow.activity.faulted";

    public const string TenantId = "elsa.tenant.id";
    public const string ExceptionType = "exception.type";

    [Obsolete("Use ExceptionType. Workflow spans follow OpenTelemetry exception semantic conventions.")]
    public const string ErrorType = ExceptionType;

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
        var workflowException = exception ?? (context.SubStatus == Workflows.WorkflowSubStatus.Faulted ? context.Exception : null);
        var cancelled = exception is OperationCanceledException || context.SubStatus == Workflows.WorkflowSubStatus.Cancelled;
        var faulted = !cancelled && (workflowException != null || context.SubStatus == Workflows.WorkflowSubStatus.Faulted);
        var workflowSubStatus = faulted ? Workflows.WorkflowSubStatus.Faulted : (Workflows.WorkflowSubStatus?)null;

        if (activity != null)
        {
            SetWorkflowTags(activity, context, workflowSubStatus);
            activity.SetTag(WorkflowFaulted, faulted);
            SetError(activity, workflowException, faulted);
            activity.Dispose();
        }

        if (faulted)
            WorkflowFaultedCounter.Add(1, CreateWorkflowTags(context, workflowSubStatusOverride: workflowSubStatus));
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
            if (faulted)
                activity.SetTag(ActivityStatus, Workflows.ActivityStatus.Faulted.ToString());
            SetActivityOutcome(activity, context);
            SetError(activity, exception ?? context.Exception, faulted);
            activity.Dispose();
        }
    }

    private static void SetWorkflowTags(DiagnosticsActivity activity, WorkflowExecutionContext context, Workflows.WorkflowSubStatus? workflowSubStatusOverride = null)
    {
        var workflow = context.Workflow;
        var identity = workflow.Identity;
        var workflowSubStatus = workflowSubStatusOverride ?? context.SubStatus;
        var workflowStatus = GetWorkflowStatus(context, workflowSubStatusOverride);

        activity.SetTag(WorkflowSystem, SystemName);
        activity.SetTag(WorkflowInstanceId, context.Id);
        activity.SetTag(WorkflowDefinitionId, identity.DefinitionId);
        activity.SetTag(WorkflowDefinitionVersion, identity.Version);
        activity.SetTag(WorkflowDefinitionVersionId, identity.Id);
        activity.SetTag(WorkflowStatus, workflowStatus.ToString());
        activity.SetTag(WorkflowSubStatus, workflowSubStatus.ToString());
        AddIfNotNull(activity, WorkflowName, workflow.WorkflowMetadata.Name ?? workflow.Name);
        AddIfNotNull(activity, WorkflowParentInstanceId, context.ParentWorkflowInstanceId);
        AddIfNotNull(activity, WorkflowCorrelationId, context.CorrelationId);
        AddIfNotNull(activity, TenantId, identity.TenantId);
    }

    private static void SetActivityTags(DiagnosticsActivity activity, ActivityExecutionContext context)
    {
        var currentActivity = context.Activity;

        activity.SetTag(ActivityId, currentActivity.Id);
        activity.SetTag(ActivityType, currentActivity.Type);
        activity.SetTag(ActivityVersion, currentActivity.Version);
        activity.SetTag(ActivityExecutionId, context.Id);
        activity.SetTag(ActivityStatus, context.Status.ToString());
        AddIfNotNull(activity, ActivityName, currentActivity.Name ?? context.ActivityDescriptor.DisplayName ?? context.ActivityDescriptor.Name);
        AddIfNotNull(activity, ActivityParentExecutionId, context.ParentActivityExecutionContext?.Id);
        AddIfNotNull(activity, ActivityScheduledByExecutionId, context.SchedulingActivityExecutionId);
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

        AddIfNotNull(activity, ActivityOutcome, outcome);
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
        {
            var exceptionType = exception.GetType().FullName;
            activity.SetTag(ExceptionType, exceptionType);
            activity.AddEvent(new("exception", tags: new ActivityTagsCollection
            {
                { ExceptionType, exceptionType }
            }));
        }
    }

    private static TagList CreateWorkflowTags(WorkflowExecutionContext context, bool includeExecutionStatus = true, Workflows.WorkflowSubStatus? workflowSubStatusOverride = null)
    {
        var workflow = context.Workflow;
        var identity = workflow.Identity;
        var workflowSubStatus = workflowSubStatusOverride ?? context.SubStatus;
        var workflowStatus = GetWorkflowStatus(context, workflowSubStatusOverride);
        var tags = new TagList
        {
            { WorkflowSystem, SystemName },
            { WorkflowDefinitionId, identity.DefinitionId },
            { WorkflowDefinitionVersion, identity.Version }
        };

        if (includeExecutionStatus)
        {
            tags.Add(WorkflowStatus, workflowStatus.ToString());
            tags.Add(WorkflowSubStatus, workflowSubStatus.ToString());
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

    private static Workflows.WorkflowStatus GetWorkflowStatus(WorkflowExecutionContext context, Workflows.WorkflowSubStatus? workflowSubStatusOverride)
    {
        if (workflowSubStatusOverride == null)
            return context.Status;

        return workflowSubStatusOverride.Value switch
        {
            Workflows.WorkflowSubStatus.Cancelled or Workflows.WorkflowSubStatus.Faulted or Workflows.WorkflowSubStatus.Finished => Workflows.WorkflowStatus.Finished,
            _ => Workflows.WorkflowStatus.Running
        };
    }

    private static void AddIfNotNull(ref TagList tags, string key, object? value)
    {
        if (value != null)
            tags.Add(key, value);
    }

    private static void AddIfNotNull(DiagnosticsActivity activity, string key, object? value)
    {
        if (value != null)
            activity.SetTag(key, value);
    }
}

internal readonly record struct WorkflowInstrumentationScope(DiagnosticsActivity? Activity);

internal readonly record struct ActivityInstrumentationScope(DiagnosticsActivity? Activity, long StartTimestamp);
