using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Triggers
{
    public interface ITriggerIndexer
    {
        Task IndexTriggersAsync(CancellationToken cancellationToken = default);
    }
}