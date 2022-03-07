using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Jobs.Contracts;

public interface IJobHandler
{
    bool GetSupports(IJob job);
    Task HandleAsync(IJob job, CancellationToken cancellationToken);
}