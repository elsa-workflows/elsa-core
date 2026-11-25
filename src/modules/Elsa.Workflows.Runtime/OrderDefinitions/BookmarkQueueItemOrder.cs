using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Entities;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.OrderDefinitions;

/// <summary>
/// Represents the order by which to order the results of a query.
/// </summary>
[UsedImplicitly]
public class BookmarkQueueItemOrder<TProp> : OrderDefinition<BookmarkQueueItem, TProp>
{
    public BookmarkQueueItemOrder(Expression<Func<BookmarkQueueItem, TProp>> keySelector, OrderDirection direction)
    {
        KeySelector = keySelector;
        Direction = direction;
    }

    internal IOrderedQueryable<BookmarkQueueItem> Apply(IQueryable<BookmarkQueueItem> items)
    {
        var ordered = Direction == OrderDirection.Ascending
            ? items.OrderBy(KeySelector)
            : items.OrderByDescending(KeySelector);

        // An ID sort already determines the order and some providers reject duplicate sort fields.
        var primaryProperty = KeySelector.GetProperty();
        if (primaryProperty?.DeclaringType == typeof(Entity) && primaryProperty.Name == nameof(Entity.Id))
            return ordered;

        return ordered.ThenBy(item => item.Id);
    }
}
