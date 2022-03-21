using System.Threading.Tasks;
using Elsa.Jobs.Contracts;
using Elsa.Jobs.Models;

namespace Elsa.Jobs.Abstractions;

public abstract class Job : IJob
{
    ValueTask IJob.ExecuteAsync(JobExecutionContext context) => ExecuteAsync(context);

    protected virtual ValueTask ExecuteAsync(JobExecutionContext context)
    {
        Execute(context);
        return ValueTask.CompletedTask;
    }

    protected virtual void Execute(JobExecutionContext context)
    {
    }
}