using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Workflows.Runtime.Entities;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.OrderDefinitions;

/// <summary>
/// Represents the order by which to order bookmark queue dead-letter results.
/// </summary>
[UsedImplicitly]
public class BookmarkQueueDeadLetterItemOrder<TProp> : OrderDefinition<BookmarkQueueDeadLetterItem, TProp>
{
    public BookmarkQueueDeadLetterItemOrder(Expression<Func<BookmarkQueueDeadLetterItem, TProp>> keySelector, OrderDirection direction)
    {
        KeySelector = keySelector;
        Direction = direction;
    }
}
