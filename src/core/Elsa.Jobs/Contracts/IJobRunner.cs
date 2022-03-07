using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Jobs.Contracts;

public interface IJobRunner
{
    Task RunJobAsync(IJob job, CancellationToken cancellationToken = default);
}