using Elsa.Jobs.Activities.Models;
using Elsa.Jobs.Contracts;
using Elsa.Workflows.Core.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Jobs.Activities;

/// <summary>
/// Executes a job of a given type, suspending execution of the workflow until the job finishes.
/// </summary>
public class JobActivity : Activity
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
        
        // TODO: It's theoretically possible that the job gets scheduled and executed immediately, even before we got a chance for the bookmark to be registered, preventing the job from resuming the workflow because the bookmark would not be found.
        // To prevent this from happening, we should implement a mechanism that allows the scheduling of the job to happen until after the bookmark has been registered.
        await jobQueue.SubmitJobAsync(job, cancellationToken: context.CancellationToken);
        var bookmarkPayload = new EnqueuedJobPayload(job.Id);
        context.CreateBookmark(bookmarkPayload, Resume);
    }

    private async ValueTask Resume(ActivityExecutionContext context)
    {
        await CompleteAsync(context);
    }
}