using System.Threading;
using System.Threading.Tasks;
using Elsa.Jobs.Contracts;

namespace Elsa.Jobs.Abstractions;

public abstract class JobHandler<T> : IJobHandler where T : IJob
{
    public bool GetSupports(IJob job) => job is T;

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