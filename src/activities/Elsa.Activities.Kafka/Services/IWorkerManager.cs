using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Activities.Kafka.Services
{
    public interface IWorkerManager
    {
        Task CreateWorkersAsync(IReadOnlyCollection<Trigger> triggers, CancellationToken cancellationToken = default);
        Task CreateWorkersAsync(IReadOnlyCollection<Bookmark> bookmarks, CancellationToken cancellationToken = default);
        Task RemoveTagsFromWorkersAsync(IReadOnlyCollection<string> tags, CancellationToken cancellationToken = default);
    }
}