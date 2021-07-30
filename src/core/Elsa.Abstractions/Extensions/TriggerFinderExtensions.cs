using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

namespace Elsa
{
    public static class TriggerFinderExtensions
    {
        public static Task<IEnumerable<TriggerFinderResult>> FindTriggersAsync<T>(
            this ITriggerFinder triggerFinder,
            IEnumerable<IBookmark> bookmarks,
            string? tenantId,
            CancellationToken cancellationToken = default) where T : IActivity =>
            triggerFinder.FindTriggersAsync(typeof(T).Name, bookmarks, tenantId, cancellationToken);

        public static Task<IEnumerable<TriggerFinderResult>> FindTriggersAsync<T>(
            this ITriggerFinder triggerFinder,
            IBookmark bookmark,
            string? tenantId,
            CancellationToken cancellationToken = default) where T : IActivity =>
            triggerFinder.FindTriggersAsync(typeof(T).Name, new[] { bookmark }, tenantId, cancellationToken);
        
        public static Task<IEnumerable<TriggerFinderResult>> FindTriggersAsync(
            this ITriggerFinder triggerFinder,
            string activityType,
            IBookmark bookmark,
            string? tenantId,
            CancellationToken cancellationToken = default) =>
            triggerFinder.FindTriggersAsync(activityType, new[] { bookmark }, tenantId, cancellationToken);

        public static Task<IEnumerable<TriggerFinderResult>> FindTriggersAsync<T>(
            this ITriggerFinder triggerFinder,
            string? tenantId,
            CancellationToken cancellationToken = default) where T : IActivity =>
            triggerFinder.FindTriggersAsync(typeof(T).Name, Enumerable.Empty<IBookmark>(), tenantId, cancellationToken);
    }
}