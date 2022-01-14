using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.File.Services
{
    public interface IFileSystemWatchersStarter
    {
        Task CreateAndAddWatchersAsync(CancellationToken cancellationToken = default);
    }
}
