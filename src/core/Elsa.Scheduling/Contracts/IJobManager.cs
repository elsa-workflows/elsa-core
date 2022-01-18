using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Scheduling.Contracts;

public interface IJobManager
{
    Task ExecuteJobAsync(IJob job, CancellationToken cancellationToken = default);
}