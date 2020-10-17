using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services
{
    public interface IStartupTask
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}