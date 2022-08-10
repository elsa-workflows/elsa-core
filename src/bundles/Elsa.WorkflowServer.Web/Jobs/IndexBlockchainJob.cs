using Elsa.Activities.Jobs.Features;
using Elsa.Jobs.Abstractions;
using Elsa.Jobs.Models;
using Elsa.Jobs.Services;

namespace Elsa.WorkflowServer.Web.Jobs;

/// <summary>
/// Jobs can be scheduled manually using <see cref="IJobQueue"/>,
/// but when enabling the <see cref="JobsFeature"/>, these jobs become available as activities too.
/// </summary>
public class IndexBlockchainJob : Job
{
    protected override async ValueTask ExecuteAsync(JobExecutionContext context)
    {
        Console.WriteLine("Indexing blockchain...");
        await Task.Delay(5000);
        Console.WriteLine("Finished indexing blockchain.");
    }
}