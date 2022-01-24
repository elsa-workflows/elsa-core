using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services
{
    public interface ITriggerFinder
    {
        Task<IEnumerable<TriggerFinderResult>> FindTriggersAsync(string activityType, IEnumerable<IBookmark> filters, string? tenantId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Trigger>> FindTriggersByTypeAsync(string modelType, string? tenantId, CancellationToken cancellationToken = default);
    }
}