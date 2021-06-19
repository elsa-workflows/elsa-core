using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services
{
    public interface IStartupRunner
    {
        Task StartupAsync(CancellationToken cancellationToken = default);
    }
}