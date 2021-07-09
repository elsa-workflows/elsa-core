using System.Threading;
using System.Threading.Tasks;

namespace Elsa.HostedServices
{
    public interface IScopedBackgroundService
    {
        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}