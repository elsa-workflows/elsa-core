using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public interface IServiceBusTopicsStarter
    {
        Task CreateWorkersAsync(CancellationToken cancellationToken = default);
    }
}