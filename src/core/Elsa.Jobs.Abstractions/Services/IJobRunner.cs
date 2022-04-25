using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Jobs.Services;

public interface IJobRunner
{
    Task RunJobAsync(IJob job, CancellationToken cancellationToken = default);
}