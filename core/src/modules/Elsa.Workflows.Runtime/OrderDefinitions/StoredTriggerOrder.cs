using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.OrderDefinitions;

public class StoredTriggerOrder<TProp> : OrderDefinition<StoredTrigger, TProp>
{
    public StoredTriggerOrder(Expression<Func<StoredTrigger, TProp>> keySelector, OrderDirection direction)
    {
        KeySelector = keySelector;
        Direction = direction;
    }
}