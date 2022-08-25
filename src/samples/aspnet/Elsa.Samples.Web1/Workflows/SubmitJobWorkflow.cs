using System;
using System.Net.Http;
using System.Threading.Tasks;
using Elsa.Jobs.Abstractions;
using Elsa.Jobs.Models;
using Elsa.Jobs.Services;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Services;

namespace Elsa.Samples.Web1.Workflows;

public class SubmitJobWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.WithRoot(new Sequence
        {
            Activities =
            {
                new WriteLine("Downloading..."),
                new Inline(async context =>
                {
                    var jobQueue = context.GetRequiredService<IJobQueue>();
                    var job = new ReadTheInternetJob();
                    await jobQueue.SubmitJobAsync(job, cancellationToken: context.CancellationToken);
                })
                // TODO: Implement a "job" activity that blocks the workflow until the job completes.
            }
        });
    }
}

public class ReadTheInternetJob : Job
{
    protected override async ValueTask ExecuteAsync(JobExecutionContext context)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://www.google.com")
        };

        var response = await httpClient.GetStringAsync("/", context.CancellationToken);
        Console.WriteLine(response);
    }
}