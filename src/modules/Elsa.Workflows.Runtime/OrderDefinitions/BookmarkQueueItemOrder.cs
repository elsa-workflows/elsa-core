using System.Linq.Expressions;
using Elsa.Common.Entities;
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
}