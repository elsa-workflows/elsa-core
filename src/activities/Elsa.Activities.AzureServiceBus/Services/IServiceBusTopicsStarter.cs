using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions.MultiTenancy;
using Elsa.Models;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public interface IServiceBusTopicsStarter
    {
        Task CreateWorkersAsync(IReadOnlyCollection<Trigger> triggers, Tenant tenant, CancellationToken cancellationToken = default);
        Task CreateWorkersAsync(IReadOnlyCollection<Bookmark> bookmarks, Tenant tenant, CancellationToken cancellationToken = default);
        Task RemoveWorkersAsync(IReadOnlyCollection<Trigger> triggers, CancellationToken cancellationToken = default);
        Task RemoveWorkersAsync(IReadOnlyCollection<Bookmark> bookmarks, CancellationToken cancellationToken = default);
    }
}