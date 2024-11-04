using Elsa.Extensions;
using Elsa.Samples.AspNet.HangfireIntegration.Jobs;
using Elsa.Samples.AspNet.HangfireIntegration.Models;
using Elsa.Samples.AspNet.HangfireIntegration.Stimuli;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Hangfire;

namespace Elsa.Samples.AspNet.HangfireIntegration.Activities;

[Activity("Samples", "Samples", "Enqueues a background job and resumes the workflow when the job is done.")]
public class ExecuteSomeJob : Activity<SomeJobResult>
{
    protected override void Execute(ActivityExecutionContext context)
    {
        var stimulus = new SomeJobStimulus(); 
        var bookmark = context.CreateBookmark(stimulus, OnResume);
        var backgroundJobClient = context.GetRequiredService<IBackgroundJobClient>();
        backgroundJobClient.Enqueue<SomeJob>(x => x.RunAsync(bookmark.Id, default));
    }

    private async ValueTask OnResume(ActivityExecutionContext context)
    {
        var jobResult = context.GetWorkflowInput<SomeJobResult>("JobResult");
        Result.Set(context, jobResult);
        await context.CompleteActivityAsync();
    }
}