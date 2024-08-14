using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.OrderDefinitions;

/// Represents the order by which to order the results of a query.
public class ActivityExecutionRecordOrder<TProp> : OrderDefinition<ActivityExecutionRecord, TProp>
{
    /// Creates a new instance of the <see cref="ActivityExecutionRecordOrder{TProp}"/> class.
    public ActivityExecutionRecordOrder(Expression<Func<ActivityExecutionRecord, TProp>> keySelector, OrderDirection direction)
    {
        KeySelector = keySelector;
        Direction = direction;
    }
}