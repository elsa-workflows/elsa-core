using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Timers.Services
{
    public interface IInstantEventManager
    {
        Task TriggerInstantEventsAsync(CancellationToken cancellationToken);
    }
}