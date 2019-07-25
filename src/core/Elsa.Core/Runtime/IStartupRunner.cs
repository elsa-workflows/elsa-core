using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Runtime
{
    public interface IStartupRunner
    {
        Task StartupAsync(CancellationToken cancellationToken = default);
    }
}