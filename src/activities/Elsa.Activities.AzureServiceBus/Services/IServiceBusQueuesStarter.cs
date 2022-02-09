using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions.MultiTenancy;
using Elsa.Models;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public interface IServiceBusQueuesStarter
    {
        Task CreateWorkersAsync(IEnumerable<Trigger> triggers, Tenant tenant, CancellationToken cancellationToken = default);
        Task CreateWorkersAsync(IEnumerable<Bookmark> bookmarks, Tenant tenant, CancellationToken cancellationToken = default);
        Task RemoveWorkersAsync(IEnumerable<Trigger> triggers, CancellationToken cancellationToken = default);
        Task RemoveWorkersAsync(IEnumerable<Bookmark> bookmarks, CancellationToken cancellationToken = default);
    }
}