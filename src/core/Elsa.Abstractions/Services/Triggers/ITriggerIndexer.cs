using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services
{
    public interface ITriggerIndexer
    {
        Task IndexTriggersAsync(CancellationToken cancellationToken = default);
    }
}