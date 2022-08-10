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
        var factory = context.GetRequiredService<IJobFactory>();
        var jobQueue = context.GetRequiredService<IJobQueue>();
        var job = factory.Create(JobType);
        await jobQueue.SubmitJobAsync(job, cancellationToken: context.CancellationToken);
        var bookmarkPayload = new EnqueuedJobPayload(job.Id);
        context.CreateBookmark(bookmarkPayload, Resume);
    }

    private async ValueTask Resume(ActivityExecutionContext context)
    {
        await CompleteAsync(context);
    }
}