using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Scheduling.Contracts;

namespace Elsa.Activities.Scheduling.Abstractions;

public abstract class JobHandler<T> : IJobHandler where T : IJob
{
    public bool Supports(IJob job) => job is T;

    public virtual Task HandleAsync(IJob job, CancellationToken cancellationToken) => HandleAsync((T)job, cancellationToken);

    protected virtual Task HandleAsync(T job, CancellationToken cancellationToken)
    {
        Handle();
        return Task.CompletedTask;
    }

    protected virtual void Handle()
    {
    }
}