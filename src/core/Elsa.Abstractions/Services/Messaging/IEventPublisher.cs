using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services
{
    public interface IEventPublisher
    {
        Task PublishAsync(object message, IDictionary<string, string>? headers = default, CancellationToken cancellationToken = default);
    }
}