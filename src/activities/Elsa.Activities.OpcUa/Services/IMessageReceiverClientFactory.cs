using Elsa.Activities.OpcUa.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.OpcUa.Services
{
    public interface IMessageReceiverClientFactory
    {
        Task<IClient> GetReceiverAsync(OpcUaBusConfiguration config, CancellationToken cancellationToken = default);
        Task DisposeReceiverAsync(IClient receiverClient, CancellationToken cancellationToken = default);
    }
}