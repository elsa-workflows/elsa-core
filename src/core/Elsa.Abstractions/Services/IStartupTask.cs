using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services
{
    public interface IStartupTask
    {
        int Order { get; }
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}