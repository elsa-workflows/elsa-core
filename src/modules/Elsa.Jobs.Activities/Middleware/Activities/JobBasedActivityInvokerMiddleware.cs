using Elsa.Jobs.Activities.Jobs;
using Elsa.Jobs.Activities.Models;
using Elsa.Jobs.Extensions;
using Elsa.Jobs.Services;
using Elsa.Workflows.Core.Middleware.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Services;

namespace Elsa.Jobs.Activities.Middleware.Activities;

public static class JobBasedActivityInvokerMiddlewareExtensions
{
    public static IActivityExecutionBuilder UseJobBasedActivityInvoker(this IActivityExecutionBuilder builder) => builder.UseMiddleware<JobBasedActivityInvokerMiddleware>();
}

/// <summary>
/// Executes the current activity from a background job if the activity is of kind <see cref="ActivityKind.Job"/> or <see cref="ActivityKind.Task"/> 
/// </summary>
public class JobBasedActivityInvokerMiddleware : DefaultActivityInvokerMiddleware
{
    internal static readonly object IsBackgroundExecution = new();
    private readonly IActivityRegistry _activityRegistry;
    private readonly IActivityDescriber _activityDescriber;
    private readonly IJobFactory _jobFactory;
    private readonly IJobQueue _jobQueue;

    public JobBasedActivityInvokerMiddleware(
        ActivityMiddlewareDelegate next,
        IActivityRegistry activityRegistry,
        IActivityDescriber activityDescriber,
        IJobFactory jobFactory,
        IJobQueue jobQueue) : base(next)
    {
        _activityRegistry = activityRegistry;
        _activityDescriber = activityDescriber;
        _jobFactory = jobFactory;
        _jobQueue = jobQueue;
    }

    protected override async ValueTask ExecuteActivityAsync(ActivityExecutionContext context)
    {
        var activity = context.Activity;
        var activityDescriptor = _activityRegistry.Find(activity.Type) ?? await _activityDescriber.DescribeActivityAsync(activity.GetType(), context.CancellationToken);
        var kind = activityDescriptor.Kind;

        var shouldRunInBackground =
            !context.TransientProperties.ContainsKey(IsBackgroundExecution) 
            && context.WorkflowExecutionContext.ExecuteDelegate == null 
            && (kind is ActivityKind.Job || (kind == ActivityKind.Task && activity.RunAsynchronously));

        // Schedule activity normally if this is not a job or task configured to run in the background.
        if (!shouldRunInBackground)
        {
            await base.ExecuteActivityAsync(context);
            return;
        }

        // Schedule a job that will execute this activity.
        var job = _jobFactory.Create<ExecuteBackgroundActivityJob>();
        var bookmarkPayload = new BackgroundActivityPayload(job.Id);
        var bookmark = context.CreateBookmark(bookmarkPayload);

        job.Activity = activity;
        job.WorkflowInstanceId = context.WorkflowExecutionContext.Id;
        job.BookmarkId = bookmark.Id;

        await _jobQueue.SubmitJobAsync(job, cancellationToken: context.CancellationToken);
    }
}