using System.Threading.Tasks;
using Elsa.Jobs.Models;
using Elsa.Jobs.Services;

namespace Elsa.Jobs.Abstractions;

/// <summary>
/// A base class for job implementations.
/// </summary>
public abstract class Job : IJob
{
    public string Id { get; set; } = default!;

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