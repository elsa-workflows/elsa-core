using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services
{
    public interface ITriggerFinder
    {
        Task<IEnumerable<TriggerFinderResult>> FindTriggersAsync(string activityType, IEnumerable<IBookmark> filters, string? tenantId = default, int skip = 0, int take = int.MaxValue, CancellationToken cancellationToken = default);
        IAsyncEnumerable<TriggerFinderResult> StreamTriggersAsync(string activityType, IEnumerable<IBookmark> filters, string? tenantId = default, CancellationToken cancellationToken = default);
        Task<IEnumerable<Trigger>> FindTriggersByTypeAsync(string modelType, string? tenantId, int skip = 0, int take = int.MaxValue, CancellationToken cancellationToken = default);
        IAsyncEnumerable<Trigger> StreamTriggersByTypeAsync(string modelType, string? tenantId, CancellationToken cancellationToken = default);
    }
}