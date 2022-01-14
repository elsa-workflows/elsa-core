using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Scheduling.Contracts;

public interface IJobHandler
{
    bool Supports(IJob job);
    Task HandleAsync(IJob job, CancellationToken cancellationToken);
}