using System;
using System.Net.Http;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.Contracts;
using Elsa.Jobs.Abstractions;
using Elsa.Jobs.Contracts;
using Elsa.Jobs.Models;
using Elsa.Runtime.Contracts;

namespace Elsa.Samples.Web1.Workflows;

public class SubmitJobWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new Inline(async context =>
        {
            var jobQueue = context.GetRequiredService<IJobQueue>();
            var job = new ReadTheInternetJob();
            await jobQueue.SubmitJobAsync(job, cancellationToken: context.CancellationToken);
        }));
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