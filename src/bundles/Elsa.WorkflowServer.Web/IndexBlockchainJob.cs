using Elsa.Jobs.Models;
using Elsa.Jobs.Services;

namespace Elsa.WorkflowServer.Web;

public class IndexBlockchainJob : IJob
{
    public async ValueTask ExecuteAsync(JobExecutionContext context)
    {
        Console.WriteLine("Indexing blockchain...");
        await Task.Delay(5000);
        Console.WriteLine("Finished indexing blockchain.");
    }
}