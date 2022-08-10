using Elsa.Activities.Jobs.Models;
using Elsa.Jobs.Services;
using Elsa.Workflows.Core.Models;

namespace Elsa.Activities.Jobs.Activities;

/// <summary>
/// Executes a given job, suspending execution of the workflow until the job finishes.
/// </summary>
public class JobActivity : ActivityBase
{
    public JobActivity()
    {
    }

    public JobActivity(Type jobType)
    {
        JobType = jobType;
    }

    public Type JobType { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var jobQueue = context.GetRequiredService<IJobQueue>();
        var job = (IJob)Activator.CreateInstance(JobType)!;
        var jobId = await jobQueue.SubmitJobAsync(job, cancellationToken: context.CancellationToken);
        var bookmarkPayload = new EnqueuedJobPayload(jobId);
        context.CreateBookmark(bookmarkPayload, Resume);
    }

    private async ValueTask Resume(ActivityExecutionContext context)
    {
        await CompleteAsync(context);
    }
}