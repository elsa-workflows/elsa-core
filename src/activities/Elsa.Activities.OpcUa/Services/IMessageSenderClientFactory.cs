using Elsa.Activities.OpcUa.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.OpcUa.Services
{
    public interface IMessageSenderClientFactory
    {
        Task<IClient> GetSenderAsync(OpcUaBusConfiguration config, CancellationToken cancellationToken = default);
        Task DisposeSenderAsync(IClient senderClient, CancellationToken cancellationToken = default);
    }
}