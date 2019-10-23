using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Runtime
{
    public interface IStartupTask
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}