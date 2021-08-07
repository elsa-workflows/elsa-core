using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services
{
    public interface ITriggerFinder
    {
        Task<IEnumerable<TriggerFinderResult>> FindTriggersAsync(string activityType, IEnumerable<IBookmark> filters, string? tenantId, CancellationToken cancellationToken = default);
    }
}