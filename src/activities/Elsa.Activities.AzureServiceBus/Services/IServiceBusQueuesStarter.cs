using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public interface IServiceBusQueuesStarter
    {
        Task CreateWorkersAsync(IEnumerable<Trigger> triggers, CancellationToken cancellationToken = default);
        Task CreateWorkersAsync(IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default);
        Task RemoveWorkersAsync(IEnumerable<Trigger> triggers, CancellationToken cancellationToken = default);
        Task RemoveWorkersAsync(IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default);
    }
}